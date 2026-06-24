using System;
using System.Collections.Generic;
using Npgsql;
using SimuladorDron.Core;

namespace SimuladorDron.Data
{
    public class AccesoPostgres
    {
        private readonly string _connectionString;

        public AccesoPostgres(string connectionString)
        {
            _connectionString = connectionString;
        }

        // DDL: Crea las tablas por código si no existen
        public void InicializarTablas()
        {
            string scriptDDL = @"
                CREATE TABLE IF NOT EXISTS tb_master_control (
                    id SERIAL PRIMARY KEY,
                    fecha_sistema TIMESTAMP WITHOUT TIME ZONE DEFAULT NOW() NOT NULL,
                    tamano_terreno_n INTEGER NOT NULL,
                    despegue_x INTEGER NOT NULL,
                    despegue_y INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS tb_det_log (
                    id SERIAL PRIMARY KEY,
                    master_id INTEGER NOT NULL,
                    etiqueta_paso INTEGER NOT NULL,
                    coordenada_x INTEGER NOT NULL,
                    coordenada_y INTEGER NOT NULL,
                    CONSTRAINT fk_det_log_master FOREIGN KEY (master_id) 
                        REFERENCES tb_master_control (id) 
                        ON DELETE CASCADE
                );";

            using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(scriptDDL, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // Guardado transaccional síncrono empleando WHILE estricto con índice manual
        public int GuardarSimulacion(int n, int startX, int startY, List<Movimiento> secuencia)
        {
            int masterId = 0;

            using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (NpgsqlTransaction trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Inserción de Cabecera usando RETURNING id y ExecuteScalar
                        string sqlMaster = @"
                            INSERT INTO tb_master_control (tamano_terreno_n, despegue_x, despegue_y) 
                            VALUES (@n, @x, @y) RETURNING id;";

                        using (NpgsqlCommand cmdMaster = new NpgsqlCommand(sqlMaster, conn, trans))
                        {
                            cmdMaster.Parameters.AddWithValue("@n", n);
                            cmdMaster.Parameters.AddWithValue("@x", startX);
                            cmdMaster.Parameters.AddWithValue("@y", startY);

                            masterId = Convert.ToInt32(cmdMaster.ExecuteScalar());
                        }

                        // Inserción atomizada uno a uno
                        string sqlDetalle = @"
                            INSERT INTO tb_det_log (master_id, etiqueta_paso, coordenada_x, coordenada_y) 
                            VALUES (@masterId, @etiqueta, @cx, @cy);";

                        // RESTRICCIÓN DE SINTAXIS: Uso exclusivo de bucle WHILE manual
                        int i = 0;
                        int cantidad = secuencia.Count;

                        while (i < cantidad)
                        {
                            Movimiento mov = secuencia[i];
                            int pasoOfuscado = 0;

                            // REGLA DE OFUSCACIÓN: Par (Paso * 2) / Impar (Negativo)
                            if (mov.Paso % 2 == 0)
                            {
                                pasoOfuscado = mov.Paso * 2;
                            }
                            else
                            {
                                pasoOfuscado = -mov.Paso;
                            }

                            using (NpgsqlCommand cmdDetalle = new NpgsqlCommand(sqlDetalle, conn, trans))
                            {
                                cmdDetalle.Parameters.AddWithValue("@masterId", masterId);
                                cmdDetalle.Parameters.AddWithValue("@etiqueta", pasoOfuscado);
                                cmdDetalle.Parameters.AddWithValue("@cx", mov.X);
                                cmdDetalle.Parameters.AddWithValue("@cy", mov.Y);

                                cmdDetalle.ExecuteNonQuery();
                            }
                            i++; // Incremento manual controlado por el programador
                        }

                        trans.Commit();
                        return masterId;
                    }
                    catch
                    {
                        trans.Rollback();
                        throw;
                    }
                }
            }
        }

        // Reporte Inverso: Lee con ExecuteReader() y hace ingeniería inversa matemática
        public void ImprimirReporteInverso(int masterId)
        {
            string sqlReporte = @"
                SELECT id, etiqueta_paso, coordenada_x, coordenada_y 
                FROM tb_det_log 
                WHERE master_id = @masterId 
                ORDER BY id DESC 
                LIMIT 5;";

            using (NpgsqlConnection conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (NpgsqlCommand cmd = new NpgsqlCommand(sqlReporte, conn))
                {
                    cmd.Parameters.AddWithValue("@masterId", masterId);

                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int detId = reader.GetInt32(0);
                            int pasoOfuscado = reader.GetInt32(1);
                            int cx = reader.GetInt32(2);
                            int cy = reader.GetInt32(3);

                            // REGLA DE RECONSTRUCCIÓN INVERSA
                            int pasoReal = 0;
                            if (pasoOfuscado < 0)
                            {
                                pasoReal = -pasoOfuscado; // Negativo pasa a Impar original
                            }
                            else
                            {
                                pasoReal = pasoOfuscado / 2; // Mayor o igual a cero se divide por 2
                            }

                            Console.WriteLine($"Detalle ID: {detId} | Celda: ({cx}, {cy}) | Ofuscado en DB: {pasoOfuscado, 4} ==> PASO RECONSTRUIDO: {pasoReal}");
                        }
                    }
                }
            }
        }
    }
}