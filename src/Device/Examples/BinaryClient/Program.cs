using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using DeviceHive.Device;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using DeviceHive.Binary;

namespace DeviceHiveTest
{
    public struct CubePixel
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte R;
        public byte G;
        public byte B;
    }

    public struct CubeColor
    {
        public byte R;
        public byte G;
        public byte B;
    }

    class Program
    {
        static async Task TestPixel(BinaryServiceHelper svc)
        {
            // Pixel
            Console.Write("Setting pixel... ");
            CubePixel cp = new CubePixel
            {
                X = 0,
                Y = 0,
                Z = 0,
                R = 0xFF,
                G = 0,
                B = 0
            };

            // C5 C3 01 00 0A 00 01 01 01 00 00 00 00 00 00 FF 00 00 6A
            BinaryServiceHelper.CommandResult cr = await svc.SendCmd("pixel", cp);
            Console.WriteLine(cr.Result + " " + cr.ExecutionTime.ToString() + " ms.");
            Thread.Sleep(1000);
            
        }

        static async Task TestFill(BinaryServiceHelper svc, CubeColor clr)
        {
            // Fill
            Console.Write("Setting the cube color... ");
            BinaryServiceHelper.CommandResult cr = await svc.SendCmd("fill", clr);
            Console.WriteLine(cr.Result + " " + cr.ExecutionTime.ToString() + " ms.");
            Thread.Sleep(1000);
        }

        static async Task TestPixels(BinaryServiceHelper svc)
        {
            // Pixels
            Console.Write("Setting random pixels... ");
            Random rnd = new Random(DateTime.Now.Millisecond);
            //while (!Console.KeyAvailable)
            //{

            CubePixel[] pxs = new CubePixel[4 * 4 * 4];

            for (byte x = 0; x < 4; x++)
            {
                for (byte y = 0; y < 4; y++)
                {
                    for (byte z = 0; z < 4; z++)
                    {
                        CubePixel cpt = new CubePixel()
                        {
                            X = (byte)x,
                            Y = (byte)y,
                            Z = (byte)z,
                            R = (byte)rnd.Next(0, 255),
                            G = (byte)rnd.Next(0, 255),
                            B = (byte)rnd.Next(0, 255)
                        };

                        pxs[x * 16 + y * 4 + z] = cpt;
                    }
                }
            }
            BinaryServiceHelper.CommandResult cr = await svc.SendCmd("pixels", pxs);
            Console.WriteLine(cr.Result + " " + cr.ExecutionTime.ToString() + " ms.");
            Thread.Sleep(1000);
        }

        static void Main(string[] args)
        {
            SerialPort com = new SerialPort("COM3", 19200, Parity.None, 8, StopBits.One);
            com.ReadTimeout = 100;
            SerialPortBinaryConnection sbc = new SerialPortBinaryConnection(com);
            BinaryServiceHelper svc = new BinaryServiceHelper(sbc);

            Console.Write("Initializing... ");
            while (!svc.IsStarted)
            {
                try
                {
                    svc.Start();
                }
                catch (Exception)
                {
                }
            }
            Console.WriteLine("done.");

            try
            {
                TestPixel(svc).Wait();

                TestFill(svc, new CubeColor()
                    {
                        R = 0,
                        G = 0xFF,
                        B = 0
                    }).Wait();
                TestPixels(svc).Wait();

                TestFill(svc, new CubeColor()
                {
                    R = 0,
                    G = 0,
                    B = 0
                }).Wait();
            }
            catch (AggregateException ex)
            {
                Console.WriteLine( ex.InnerException.ToString());
                Console.ReadKey();
            }
            
            svc.Stop();
            svc.Dispose();
            sbc.Dispose();
            com.Dispose();
        }
    }
}
