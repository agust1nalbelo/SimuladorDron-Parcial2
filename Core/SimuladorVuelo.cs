using System;
using System.Collections.Generic;

namespace SimuladorDron.Core
{
    public class SimuladorVuelo
    {
        private readonly int N;
        private readonly int[,] matriz;
        private readonly int despegueX;
        private readonly int despegueY;
        private int totalAlcanzables;

        private readonly int[] dx = { -2, -2, 2, 2, -1, -1, 1, 1 };
        private readonly int[] dy = { -1, 1, -1, 1, -2, 2, -2, 2 };

        public List<Movimiento> SecuenciaMovimientos { get; private set; }

        public SimuladorVuelo(int n, int startX, int startY)
        {
            N = n;
            despegueX = startX;
            despegueY = startY;
            matriz = new int[N, N];
            SecuenciaMovimientos = new List<Movimiento>();

            int i = 0;
            while (i < N)
            {
                int j = 0;
                while (j < N)
                {
                    matriz[i, j] = -1;
                    j++;
                }
                i++;
            }
        }

        private bool EsValido(int x, int y)
        {
            return (x >= 0 && x < N && y >= 0 && y < N);
        }

        public void CalcularAlcanzables()
        {
            bool[,] visitado = new bool[N, N];
            Queue<(int, int)> fila = new Queue<(int, int)>();

            fila.Enqueue((despegueX, despegueY));
            visitado[despegueX, despegueY] = true;
            totalAlcanzables = 0;

            while (fila.Count > 0)
            {
                var (cx, cy) = fila.Dequeue();
                totalAlcanzables++;

                int i = 0;
                while (i < 8)
                {
                    int nx = cx + dx[i];
                    int ny = cy + dy[i];

                    if (EsValido(nx, ny) && !visitado[nx, ny])
                    {
                        visitado[nx, ny] = true;
                        fila.Enqueue((nx, ny));
                    }
                    i++;
                }
            }
        }

        private int ObtenerGrado(int x, int y)
        {
            int count = 0;
            int i = 0;
            while (i < 8)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                if (EsValido(nx, ny) && matriz[nx, ny] == -1)
                {
                    count++;
                }
                i++;
            }
            return count;
        }

        public bool Resolver()
        {
            CalcularAlcanzables();
            
            matriz[despegueX, despegueY] = 0;
            SecuenciaMovimientos.Add(new Movimiento { X = despegueX, Y = despegueY, Paso = 0 });

            return ResolverRecursivo(despegueX, despegueY, 1);
        }

        private bool ResolverRecursivo(int cx, int cy, int pasoActual)
        {
            if (pasoActual == totalAlcanzables)
            {
                return true;
            }

            List<(int nextX, int nextY, int grado)> candidatos = new List<(int, int, int)>();

            int i = 0;
            while (i < 8)
            {
                int nx = cx + dx[i];
                int ny = cy + dy[i];

                if (EsValido(nx, ny) && matriz[nx, ny] == -1)
                {
                    int grado = ObtenerGrado(nx, ny);
                    candidatos.Add((nx, ny, grado));
                }
                i++;
            }

            candidatos.Sort((a, b) => a.grado.CompareTo(b.grado));

            int idxCandidato = 0;
            int totalCandidatos = candidatos.Count;

            while (idxCandidato < totalCandidatos)
            {
                var candidato = candidatos[idxCandidato];
                int nx = candidato.nextX;
                int ny = candidato.nextY;

                matriz[nx, ny] = pasoActual;
                SecuenciaMovimientos.Add(new Movimiento { X = nx, Y = ny, Paso = pasoActual });

                if (ResolverRecursivo(nx, ny, pasoActual + 1))
                {
                    return true;
                }

                matriz[nx, ny] = -1;
                SecuenciaMovimientos.RemoveAt(SecuenciaMovimientos.Count - 1);

                idxCandidato++;
            }

            return false;
        }

        // CORREGIDO: Muestra la matriz de manera compacta alineada con tabulaciones
        public void DibujarMatriz()
        {
            int i = 0;
            while (i < N)
            {
                int j = 0;
                while (j < N)
                {
                    if (matriz[i, j] == -1)
                        Console.Write(".\t");
                    else
                        Console.Write($"{matriz[i, j]}\t");
                    j++;
                }
                Console.WriteLine();
                i++;
            }
        }
    }
}