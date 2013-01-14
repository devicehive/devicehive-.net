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
		private readonly IBinaryConnection _connection;

		#region Private fields

		private const byte _version = 1;

		private readonly MessageReaderWriter _messageReaderWriter;
		private readonly ILog _logger;

		private IDictionary<ushort, NotificationInfo> _notificationMapping;
		private IDictionary<string, CommandInfo> _commandMapping; 

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

			connection.DataAvailable += (s, e) =>
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
			};
		}

		#endregion		

		#region Protected methods

		#region Abstract methods (handle messages)

		protected abstract void RegisterDevice(DeviceRegistrationInfo registrationInfo);

		protected abstract void NotifyCommandResult(int commandId, string status, string result);

		protected abstract void HandleHotification(Notification notification);

		#endregion

		#region Send messages

		protected void RequestRegistration()
		{
			SendMessage(Intents.RequestRegistration, new byte[0]);
		}

		protected void SendCommand(Command command)
		{
			CommandInfo commandInfo;
			if (!_commandMapping.TryGetValue(command.Name, out commandInfo))
				throw new InvalidOperationException(string.Format("Command {0} is not registered", command.Name));

			byte[] data;

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(command.Id.Value);

				foreach (var parameterInfo in commandInfo.Parameters)
					WriteParameterValue(writer, parameterInfo, command.Parameters);

				data = stream.ToArray();
			}

			SendMessage(commandInfo.Intent, data);
		}

		#endregion

		#endregion

		#region Private methods

		#region Handle messages

		private void HandleMessage(Message message)
		{
			if (message.Version != _version)
				throw new InvalidOperationException("Invalid message version: " + message.Version);

			if (message.Intent == Intents.Register)
			{
				RegisterDevice(message.Data);
				return;
			}

			if (message.Intent == Intents.NotifyCommandResult)
			{
				NotifyCommandResult(message.Data);
				return;
			}

			// in other case message should be custom notification message
			HandleNotification(message.Intent, message.Data);
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

			_notificationMapping = registrationInfo.Notifications.ToDictionary(n => n.Intent);
			_commandMapping = registrationInfo.Commands.ToDictionary(c => c.Name);

			RegisterDevice(registrationInfo);
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
			NotificationInfo notificationInfo;
			if (!_notificationMapping.TryGetValue(intent, out notificationInfo))
				throw new InvalidOperationException("Unsupported intent: " + intent);

			JToken parameters;

			using (var stream = new MemoryStream(data))
			using (var reader = new BinaryReader(stream))
			{
				parameters = new JObject(notificationInfo.Parameters
					.Select(p => ReadParameterValue(reader, p))
					.ToArray());
			}

			var notification = new Notification(notificationInfo.Name, parameters);
			HandleHotification(notification);
		}

		#endregion

		#region Send messages

		private void SendMessage(ushort intent, byte[] data)
		{
			var message = new Message(_version, 0, intent, data);
			_messageReaderWriter.WriteMessage(message);
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

		private static NotificationInfo ReadNotificationInfo(BinaryReader reader)
		{
			return new NotificationInfo()
			{
				Intent = reader.ReadUInt16(),
				Name = reader.ReadUtfString(),
				Parameters = reader.ReadArray(ReadParameterInfo)
			};
		}

		private static CommandInfo ReadCommandInfo(BinaryReader reader)
		{
			return new CommandInfo()
			{
				Intent = reader.ReadUInt16(),
				Name = reader.ReadUtfString(),
				Parameters = reader.ReadArray(ReadParameterInfo)
			};
		}

		private static ParameterInfo ReadParameterInfo(BinaryReader reader)
		{
			return new ParameterInfo()
			{
				DataType = (DataType) reader.ReadByte(),
				Name = reader.ReadUtfString()
			};
		}

		private static object ReadParameterValue(BinaryReader reader, ParameterInfo parameterInfo)
		{
			switch (parameterInfo.DataType)
			{
				case DataType.Null:
					return null;

				case DataType.Byte:
					return reader.ReadByte();
				
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

				default:
					throw new NotSupportedException(parameterInfo.DataType + " is not supported for parameters");
			}
		}

		#endregion

		#region Write data

		private static void WriteParameterValue(BinaryWriter writer, ParameterInfo parameterInfo, JToken parameters)
		{
			JToken value = null;

			if (parameters != null && parameters.Type == JTokenType.Object)
				value = parameters[parameterInfo.Name];

			switch (parameterInfo.DataType)
			{
				case DataType.Null:
					break;

				case DataType.Byte:
					writer.Write((byte) (value ?? 0));
					break;

				case DataType.Word:
					writer.Write((ushort) (value ?? 0));
					break;

				case DataType.Dword:
					writer.Write((uint) (value ?? 0));
					break;

				case DataType.Qword:
					writer.Write((ulong) (value ?? 0));
					break;

				case DataType.SignedByte:
					writer.Write((sbyte) (value ?? 0));
					break;

				case DataType.SignedWord:
					writer.Write((short) (value ?? 0));
					break;

				case DataType.SignedDword:
					writer.Write((int) (value ?? 0));
					break;

				case DataType.SignedQword:
					writer.Write((long) (value ?? 0));
					break;

				case DataType.Single:
					writer.Write((float) (value ?? 0));
					break;

				case DataType.Double:
					writer.Write((double) (value ?? 0));
					break;

				case DataType.Boolean:
					var boolValue = (bool) (value ?? false);
					writer.Write((byte) (boolValue ? 1 : 0));
					break;

				case DataType.Guid:
					writer.WriteGuid((Guid) (value ?? Guid.Empty));
					break;

				case DataType.UtfString:
					writer.Write((string) (value ?? string.Empty));
					break;

				case DataType.Binary:
					writer.Write((ushort) (value ?? new byte[0]));
					break;

				default:
					throw new NotSupportedException(parameterInfo.DataType + " is not supported for parameters");
			}
		}

		#endregion

		#endregion

		#region Implementation of IDisposable

		public void Dispose()
		{
			_connection.Dispose();
		}

		#endregion
	}
}