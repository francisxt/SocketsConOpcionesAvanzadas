using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ServidorEcoConTimeout
{
    class Program
    {
        private const int TAM_BUFFER = 32;
        private const int TAM_COLA = 5;
        private const int LIMITE_ESPERA = 10000;
        static void Main(string[] args)
        {
            int puerto = 8080;
            Socket servidor = null;
            try
            {
                servidor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                servidor.Bind(new IPEndPoint(IPAddress.Any, puerto));
                servidor.Listen(TAM_COLA);
            }
            catch (SocketException se) { Console.WriteLine(se.ErrorCode + ": " + se.Message); Environment.Exit(se.ErrorCode); }
            byte[] buferRx = new byte[TAM_BUFFER];
            int cantBytesRecibidos;
            int totalBytesEnviados = 0;
            for (; ; )
            {
                Socket cliente = null;
                try
                {
                    cliente = servidor.Accept();
                    DateTime tiempoInicio = DateTime.Now;
                    cliente.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, LIMITE_ESPERA);
                    Console.Write("Gestionando al cliente: " + cliente.RemoteEndPoint + " - ");
                    totalBytesEnviados = 0;
                    while ((cantBytesRecibidos = cliente.Receive(buferRx, 0, buferRx.Length, SocketFlags.None)) > 0)
                    {
                        cliente.Send(buferRx, 0, cantBytesRecibidos, SocketFlags.None);
                        totalBytesEnviados += cantBytesRecibidos;
                        TimeSpan tiempoTranscurrido = DateTime.Now - tiempoInicio;
                        if (LIMITE_ESPERA - tiempoTranscurrido.TotalMilliseconds < 0)
                        {
                            Console.WriteLine("Terminando la conexión con el cliente debido al temporizador. Se han superado los " + LIMITE_ESPERA + "ms; se han enviado " + totalBytesEnviados + " bytes");
                            cliente.Close();
                            throw new SocketException(10060);
                        }
                        cliente.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, (int)(LIMITE_ESPERA - tiempoTranscurrido.TotalMilliseconds));
                    }
                    Console.WriteLine("Se han enviado {0} bytes.", totalBytesEnviados);
                    cliente.Close();
                }
                catch (SocketException se)
                {
                    if (se.ErrorCode == 10060)
                    {
                        Console.WriteLine("Terminado la conexion debido al temporizador. Han transcurrido " + LIMITE_ESPERA + "ms; se han transmitido " + totalBytesEnviados + " bytes");
                    }
                    else { Console.WriteLine(se.ErrorCode + ": " + se.Message); } cliente.Close();
                }
            }
        }
    }
}