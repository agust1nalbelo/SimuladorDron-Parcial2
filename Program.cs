using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using SimuladorDron.Core;
using SimuladorDron.Data;

namespace SimuladorDron
{
    class Program
    {
        static void Main(string[] args)
        {
            // Inicializar el ConfigurationBuilder para leer la cadena antes de tocar la base de datos
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration configuration = builder.Build();
            string connectionString = configuration.GetConnectionString("PostgresConnection") 
                ?? throw new InvalidOperationException("Falta la cadena de conexión en appsettings.json.");

            // Inicialización de la persistencia y validación DDL automática
            AccesoPostgres capaDatos = new AccesoPostgres(connectionString);
            capaDatos.InicializarTablas();

            // Interfaz de Usuario y Validaciones en Tiempo de Ejecución (Parte E)
            int n = 0;
            while (true)
            {
                Console.Write("\nDimensión del espacio N (entero >= 1): ");
                if (int.TryParse(Console.ReadLine(), out n) && n >= 1) 
                {
                    break;
                }
                Console.WriteLine("Error: El tamaño del terreno debe ser entero >= 1.");
            }

            int startX = 0;
            while (true)
            {
                Console.Write("Coordenada inicial de despegue X (Fila): ");
                if (int.TryParse(Console.ReadLine(), out startX) && startX >= 0 && startX < n) 
                {
                    break;
                }
                Console.WriteLine($"Error: Coordenada X fuera de rango [0, {n - 1}].");
            }

            int startY = 0;
            while (true)
            {
                Console.Write("Coordenada inicial de despegue Y (Columna): ");
                if (int.TryParse(Console.ReadLine(), out startY) && startY >= 0 && startY < n) 
                {
                    break;
                }
                Console.WriteLine($"Error: Coordenada Y fuera de rango [0, {n - 1}].");
            }

            Console.WriteLine("\nIniciando simulación del recorrido recursivo...");
            SimuladorVuelo simulador = new SimuladorVuelo(n, startX, startY);

            if (!simulador.Resolver())
            {
                Console.WriteLine("\n[SIN SOLUCIÓN]: No existe ruta válida.");
                simulador.DibujarMatriz();
                return;
            }

            // Mostrar el recorrido calculado de forma numérica ordinaria en consola
            Console.WriteLine("\nSimulación Exitosa! Matriz del Recorrido:\n");
            simulador.DibujarMatriz();

            // Guardar en la base de datos de Docker
            try
            {
                int idEjecucion = capaDatos.GuardarSimulacion(n, startX, startY, simulador.SecuenciaMovimientos);
                Console.WriteLine($"\n[PERSISTENCIA OK]: Simulación guardada. ID asignado: {idEjecucion}");

                // Consulta final e Ingeniería Inversa en caliente
                Console.WriteLine("\n================================================================================");
                Console.WriteLine("REPORTE INVERSO: ÚLTIMOS 5 REGISTROS RECONSTRUIDOS");
                Console.WriteLine("================================================================================");
                capaDatos.ImprimirReporteInverso(idEjecucion);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR CRÍTICO EN PERSISTENCIA]: {ex.Message}");
            }
        }
    }
}