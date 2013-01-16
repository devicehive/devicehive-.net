using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeviceHive.Device;
using Newtonsoft.Json.Linq;
using log4net;

namespace DeviceHive.Binary
{
	/// <summary>
	/// Base class for services that work with device using binary DeviceHive protocol
	/// </summary>
	public abstract class BinaryServiceBase : IDisposable
	{
		#region Private fields

		private const byte _version = 1;

	    private readonly object _lock = new object();

		private readonly MessageReaderWriter _messageReaderWriter;
		private readonly ILog _logger;
        private readonly IBinaryConnection _connection;

		private IDictionary<ushort, NotificationMetadata> _notificationMapping;
		private IDictionary<string, CommandMetadata> _commandMapping; 

		#endregion

		#region Constructor

        /// <summary>
        /// Initialize new instance of <see cref="BinaryServiceBase"/>
        /// </summary>
		protected BinaryServiceBase(IBinaryConnection connection)
		{
			_connection = connection;
			_messageReaderWriter = new MessageReaderWriter(connection);
			_logger = LogManager.GetLogger(GetType());			
		}	    

	    #endregion

        #region Public methods and properties

        /// <summary>
        /// Gets flag indicating that service is started
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Start listening messages from device
        /// </summary>
        public virtual void Start()
        {
            if (IsStarted)
                return;

            lock (_lock)
            {
                if (IsStarted)
                    return;

                _connection.DataAvailable += OnDeviceDataAvailable;
                _connection.Connect();
                IsStarted = true;
            }
        }

        /// <summary>
        /// Stop listening messages from device
        /// </summary>
        public virtual void Stop()
        {
            if (!IsStarted)
                return;

            lock (_lock)
            {
                if (!IsStarted)
                    return;

                _connection.DataAvailable -= OnDeviceDataAvailable;
                _connection.Dispose();
                IsStarted = false;
            }
        }
        
        #endregion

        #region Protected methods

        #region Abstract methods (handle messages)

        /// <summary>
        /// Override it to handle device registration in the service specific way
        /// </summary>
		protected abstract void RegisterDevice(DeviceRegistrationInfo registrationInfo);

        /// <summary>
        /// Override it to handle notification from device about commandexecution result in the
        /// service specific way
        /// </summary>
		protected abstract void NotifyCommandResult(int commandId, string status, string result);

        /// <summary>
        /// Override it to handle device custom notification in the service specific way
        /// </summary>
		protected abstract void HandleHotification(Notification notification);

		#endregion

		#region Send messages

        /// <summary>
        /// Send "Request registration" command to the device
        /// </summary>
		protected void RequestRegistration()
		{
            SendMessage(Intents.RequestRegistration, new byte[0]);
		}

        /// <summary>
        /// Send custom DeviceHive command to the device
        /// </summary>
		protected void SendCommand(Command command)
		{
			CommandMetadata commandMetadata;
			if (!_commandMapping.TryGetValue(command.Name, out commandMetadata))
				throw new InvalidOperationException(string.Format("Command {0} is not registered", command.Name));

			byte[] data;

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(command.Id.Value);
                WriteParameterValue(writer, commandMetadata.Parameters, command.Parameters);
				data = stream.ToArray();
			}

			SendMessage(commandMetadata.Intent, data);
		}

		#endregion

		#endregion

		#region Private methods

		#region Handle messages

        private void OnDeviceDataAvailable(object sender, EventArgs eventArgs)
        {
            try
            {
                var message = _messageReaderWriter.ReadMessage();
                HandleMessage(message);
            }
            catch (Exception ex)
            {
                _logger.Error("Message handle error", ex);
            }
        }

		private void HandleMessage(Message message)
		{
			if (message.Version != _version)
				throw new InvalidOperationException("Invalid message version: " + message.Version);

		    switch (message.Intent)
		    {
		        case Intents.Register:
                    RegisterDevice(message.Data);
		            break;

                case Intents.Register2:
                    RegisterDevice2(message.Data);
                    break;

                case Intents.NotifyCommandResult:
                    NotifyCommandResult(message.Data);
		            break;

                default: // in other case message should be custom notification message
                    HandleNotification(message.Intent, message.Data);
		            break;
		    }
		}

	    private void RegisterDevice(byte[] data)
		{
			var registrationInfo = new DeviceRegistrationInfo();

			using (var stream = new MemoryStream(data))
			using (var reader = new BinaryReader(stream))
			{
				registrationInfo.Id = reader.ReadGuid();
				registrationInfo.Key = reader.ReadUtfString();
				registrationInfo.Name = reader.ReadUtfString();
				registrationInfo.ClassName = reader.ReadUtfString();
				registrationInfo.ClassVersion = reader.ReadUtfString();
				registrationInfo.Equipment = reader.ReadArray(ReadEquipmentInfo);
				registrationInfo.Notifications = reader.ReadArray(ReadNotificationInfo);
				registrationInfo.Commands = reader.ReadArray(ReadCommandInfo);
			}

            RegisterDeviceCore(registrationInfo);
		}

        private void RegisterDevice2(byte[] data)
        {
            JObject jsonData;

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
                jsonData = JObject.Parse(reader.ReadUtfString());

            var registrationInfo = new DeviceRegistrationInfo();
            registrationInfo.Id = (Guid) jsonData["id"];
            registrationInfo.Key = (string) jsonData["key"];
            registrationInfo.Name = (string) jsonData["name"];

            var deviceClassJson = (JObject) jsonData["deviceClass"];
            registrationInfo.ClassName = (string) deviceClassJson["name"];
            registrationInfo.ClassVersion = (string) deviceClassJson["version"];

            var equipmentJson = (JArray) jsonData["equipment"];
            if (equipmentJson == null || equipmentJson.Count == 0)
            {
                registrationInfo.Equipment = new EquipmentInfo[0];
            }
            else
            {
                registrationInfo.Equipment = equipmentJson
                    .Select(e => new EquipmentInfo()
                    {
                        Code = (string) e["code"],
                        Name = (string) e["name"],
                        TypeName = (string) e["type"]
                    })
                    .ToArray();
            }

            var commandsJson = (JArray) jsonData["commands"];
            if (commandsJson == null || commandsJson.Count == 0)
            {
                registrationInfo.Commands = new CommandMetadata[0];
            }
            else
            {
                registrationInfo.Commands = commandsJson
                    .Select(c => new CommandMetadata()
                    {
                        Intent = (ushort) c["intent"],
                        Name = (string) c["name"],
                        Parameters = ParseParameterMetadata(null, c["params"])
                    })
                    .ToArray();
            }

            var notificationsJson = (JArray)jsonData["notifications"];
            if (notificationsJson == null || notificationsJson.Count == 0)
            {
                registrationInfo.Notifications = new NotificationMetadata[0];
            }
            else
            {
                registrationInfo.Notifications = notificationsJson
                    .Select(c => new NotificationMetadata()
                    {
                        Intent = (ushort) c["intent"],
                        Name = (string) c["name"],
                        Parameters = ParseParameterMetadata(null, c["params"])
                    })
                    .ToArray();
            }

            RegisterDeviceCore(registrationInfo);
        }

        private void RegisterDeviceCore(DeviceRegistrationInfo deviceRegistrationInfo)
        {
            _notificationMapping = deviceRegistrationInfo.Notifications.ToDictionary(n => n.Intent);
            _commandMapping = deviceRegistrationInfo.Commands.ToDictionary(c => c.Name);
            RegisterDevice(deviceRegistrationInfo);
        }

		private void NotifyCommandResult(byte[] data)
		{
			int commandId;
			string status;
			string result;

			using (var stream = new MemoryStream(data))
			using (var reader = new BinaryReader(stream))
			{
				commandId = reader.ReadInt32();
				status = reader.ReadUtfString();
				result = reader.ReadUtfString();
			}

			NotifyCommandResult(commandId, status, result);
		}

		private void HandleNotification(ushort intent, byte[] data)
		{
			NotificationMetadata notificationMetadata;
			if (!_notificationMapping.TryGetValue(intent, out notificationMetadata))
				throw new InvalidOperationException("Unsupported intent: " + intent);

			JToken parameters;

			using (var stream = new MemoryStream(data))
			using (var reader = new BinaryReader(stream))
			    parameters = ReadParameterValue(reader, notificationMetadata.Parameters);

		    var notification = new Notification(notificationMetadata.Name, parameters);
			HandleHotification(notification);
		}

		#endregion

		#region Send messages

		private void SendMessage(ushort intent, byte[] data)
		{
            try
            {
                var message = new Message(_version, 0, intent, data);
                _messageReaderWriter.WriteMessage(message);
            }
            catch (Exception e)
            {
                _logger.Error("Error occurred on message sending", e);
            }
		}

		#endregion

		#region Read data

		private static EquipmentInfo ReadEquipmentInfo(BinaryReader reader)
		{
			return new EquipmentInfo()
			{
				Name = reader.ReadUtfString(),
				Code = reader.ReadUtfString(),
				TypeName = reader.ReadUtfString()
			};
		}

		private static NotificationMetadata ReadNotificationInfo(BinaryReader reader)
		{
			return new NotificationMetadata()
			{
				Intent = reader.ReadUInt16(),
				Name = reader.ReadUtfString(),
				Parameters = ReadParameterList(reader)
			};
		}

		private static CommandMetadata ReadCommandInfo(BinaryReader reader)
		{
			return new CommandMetadata()
			{
				Intent = reader.ReadUInt16(),
				Name = reader.ReadUtfString(),
				Parameters = ReadParameterList(reader)
			};
		}

        private static ParameterMetadata ReadParameterList(BinaryReader reader)
        {
            return new ParameterMetadata(null, DataType.Object,
                reader.ReadArray(r => new ParameterMetadata(
                    reader.ReadUtfString(),
                    (DataType) reader.ReadByte())));
        }

		private static JToken ReadParameterValue(BinaryReader reader, ParameterMetadata parameterMetadata)
		{
			switch (parameterMetadata.DataType)
			{
				case DataType.Null:
					return null;

				case DataType.Byte:
					return (int) reader.ReadByte();
				
				case DataType.Word:
					return reader.ReadUInt16();

				case DataType.Dword:
					return reader.ReadUInt32();

				case DataType.Qword:
					return reader.ReadUInt64();

				case DataType.SignedByte:
					return reader.ReadSByte();

				case DataType.SignedWord:
					return reader.ReadInt16();

				case DataType.SignedDword:
					return reader.ReadInt32();
				
				case DataType.SignedQword:
					return reader.ReadInt64();

				case DataType.Single:
					return reader.ReadSingle();

				case DataType.Double:
					return reader.ReadDouble();

				case DataType.Boolean:
					return reader.ReadByte() != 0;

				case DataType.Guid:
					return reader.ReadGuid();

				case DataType.UtfString:
					return reader.ReadUtfString();

				case DataType.Binary:
					return reader.ReadBinary();

                case DataType.Array:
			        return new JArray(reader
                        .ReadArray(r => ReadParameterValue(r, parameterMetadata.Children[0]))
                        .Cast<object>()
                        .ToArray());

                case DataType.Object:
			        return new JObject(parameterMetadata.Children
			            .Select(p => new JProperty(p.Name, ReadParameterValue(reader, p)))
                        .Cast<object>()
			            .ToArray());

				default:
					throw new NotSupportedException(parameterMetadata.DataType + " is not supported for parameters");
			}
		}

		#endregion

        #region Parsing data

        private static ParameterMetadata ParseParameterMetadata(string name, JToken token)
        {
            if (token == null)
                return new ParameterMetadata(name, DataType.Null);

            switch (token.Type)
            {
                case JTokenType.None:
                case JTokenType.Null:
                    return new ParameterMetadata(name, DataType.Null);

                case JTokenType.Object:
                    return new ParameterMetadata(name, DataType.Object, ((JObject) token)
                        .Properties()
                        .Select(p => ParseParameterMetadata(p.Name, p.Value))
                        .ToArray());

                case JTokenType.Array:
                    var array = (JArray) token;
                    if (array.Count != 1)
                        throw new InvalidOperationException("Invalid size of array metadata (should be 1)");

                    return new ParameterMetadata(name, DataType.Array,
                        new[] {ParseParameterMetadata(null, array[0])});

                case JTokenType.String:
                    return new ParameterMetadata(name, ParsePrimitiveDataType((string) token));
                
                default:
                    throw new InvalidOperationException("Can't parse ParameterMetadata from " + token.Type);
            }
        }

        private static DataType ParsePrimitiveDataType(string dataType)
        {
            switch (dataType)
            {
                case "bool":
                    return DataType.Boolean;

                case "u8":
                case "uint8":
                    return DataType.Byte;

                case "i8":
                case "int8":
                    return DataType.SignedByte;

                case "u16":
                case "uint16":
                    return DataType.Word;

                case "i16":
                case "int16":
                    return DataType.SignedWord;

                case "u32":
                case "uint32":
                    return DataType.Dword;

                case "i32":
                case "int32":
                    return DataType.SignedDword;

                case "u64":
                case "uint64":
                    return DataType.Qword;

                case "i64":
                case "int64":
                    return DataType.SignedQword;

                case "f":
                case "single":
                    return DataType.Single;

                case "ff":
                case "double":
                    return DataType.Double;

                case "uuid":
                case "guid":
                    return DataType.Guid;

                case "s":
                case "str":
                case "string":
                    return DataType.UtfString;

                case "b":
                case "bin":
                case "binary":
                    return DataType.Binary;

                default:
                    throw new InvalidOperationException("Unknown primitive type: " + dataType);
            }
        }

        #endregion

        #region Write data

        private static void WriteParameterValue(BinaryWriter writer,
            ParameterMetadata parameterMetadata, JToken parameter)
		{
			switch (parameterMetadata.DataType)
			{
				case DataType.Null:
					break;

			    case DataType.Byte:
			        writer.Write((byte) (parameter ?? 0));
			        break;

			    case DataType.Word:
			        writer.Write((ushort) (parameter ?? 0));
			        break;

			    case DataType.Dword:
			        writer.Write((uint) (parameter ?? 0));
			        break;

			    case DataType.Qword:
			        writer.Write((ulong) (parameter ?? 0));
			        break;

			    case DataType.SignedByte:
			        writer.Write((sbyte) (parameter ?? 0));
			        break;

			    case DataType.SignedWord:
			        writer.Write((short) (parameter ?? 0));
			        break;

			    case DataType.SignedDword:
			        writer.Write((int) (parameter ?? 0));
			        break;

			    case DataType.SignedQword:
			        writer.Write((long) (parameter ?? 0));
			        break;

			    case DataType.Single:
			        writer.Write((float) (parameter ?? 0));
			        break;

			    case DataType.Double:
			        writer.Write((double) (parameter ?? 0));
			        break;

			    case DataType.Boolean:
			        var boolValue = (bool) (parameter ?? false);
			        writer.Write((byte) (boolValue ? 1 : 0));
			        break;

			    case DataType.Guid:
			        writer.WriteGuid((Guid) (parameter ?? Guid.Empty));
			        break;

			    case DataType.UtfString:
			        writer.WriteUtfString((string) (parameter ?? string.Empty));
			        break;

			    case DataType.Binary:
			        writer.WriteBinary((byte[]) (parameter ?? new byte[0]));
			        break;

                case DataType.Array:
			        WriteArrayParameterValue(writer, parameterMetadata, parameter);
			        break;

                case DataType.Object:
			        WriteObjectParameterValue(writer, parameterMetadata, parameter);
			        break;

			    default:
					throw new NotSupportedException(parameterMetadata.DataType + " is not supported for parameters");
			}
		}
	    
	    private static void WriteArrayParameterValue(BinaryWriter writer,
            ParameterMetadata parameterMetadata, JToken parameter)
	    {
            if (parameter == null)
            {
                writer.Write(0);
                return;
            }

	        var array = (JArray) parameter;
	        writer.Write((ushort) array.Count);

	        foreach (var item in array)
	            WriteParameterValue(writer, parameterMetadata.Children[0], item);
	    }

        private static void WriteObjectParameterValue(BinaryWriter writer,
            ParameterMetadata parameterMetadata, JToken parameter)
        {
            var obj = (JObject) (parameter ?? new JObject());
            foreach (var childParameterMetadata in parameterMetadata.Children)
            {
                var childParameter = obj[childParameterMetadata.Name];
                WriteParameterValue(writer, childParameterMetadata, childParameter);
            }
        }


		#endregion

		#endregion

		#region Implementation of IDisposable

	    /// <summary>
	    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
	    /// </summary>
	    /// <filterpriority>2</filterpriority>
	    public void Dispose()
		{
			_connection.Dispose();
		}

		#endregion
	}
}