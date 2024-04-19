using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

class Program
{
    static void Main()
    {
        try
        {
            SqlConnection cn = new SqlConnection();
            cn.ConnectionString = "Banco de dados";
            cn.Open();

            string strSQL = "SELECT * FROM tbl_processo Upload WHERE status='PENDENTE' ORDER BY codigo";

            SqlCommand cmd = new SqlCommand(strSQL, cn);
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = strSQL;

            SqlDataReader dr = cmd.ExecuteReader();

            while (dr.Read())
            {
                try
                {
                    string storageType = dr["storage"].ToString();
                    string caminhoOrigem = dr["caminhoOrigem"].ToString();
                    int processoUploadId = Convert.ToInt32(dr["codigo"]);
                    string caminhoDestino = dr["caminhoDestino"].ToString();

                    string comandoCopy = "";
                    string diretorioRClone = @"C:\caminho_do_Rclone\rclone-v1.65.0-windows-amd64";
                    string x = "GoogleDrive";
                    if (storageType == x)
                    {
                        comandoCopy = $"rclone copy --ignore-times -v \"{caminhoOrigem}\" \"{caminhoDestino}\" --log-file \"C:\\caminho_log\\rclone.log\"";
                    }
                    else
                    {
                        Console.WriteLine($"Tipo de storage não reconhecido para o processo {processoUploadId}");
                        continue;
                    }

                    using (Process process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            CreateNoWindow = false,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            WorkingDirectory = diretorioRClone,
                            Arguments = $"/C {comandoCopy}"
                        }
                    })
                    {
                        process.Start();
                        process.WaitForExit();

                        string statusAtualizado = "";

                        if (process.ExitCode == 0)
                        {
                            string logFilePath = @"C:\caminho_log\rclone.log";
                            string logContent = File.ReadAllText(logFilePath);

                            Console.WriteLine("Conteúdo do log do rclone:");
                            Console.WriteLine(logContent);

                            if (logContent.Contains("Transferred") && logContent.Contains("100%"))
                            {
                                File.Delete(caminhoOrigem);
                                Console.WriteLine("Arquivo ou pasta excluído da origem.");
                                statusAtualizado = "Processado";
                            }
                            else if (logContent.Contains("ERROR") || logContent.Contains("failed"))
                            {
                                Console.WriteLine("Falha no upload! O log contém mensagens de erro.");
                                statusAtualizado = "Falha";
                            }
                            else
                            {
                                Console.WriteLine("Falha no upload! O log não contém mensagens indicando sucesso.");
                                statusAtualizado = "Falha";
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Erro ao executar o comando rclone copy. Código de saída: {process.ExitCode}");
                            statusAtualizado = "Erro";
                        }

                        string updateQuery = "UPDATE tbl_processoUpload SET status = @status WHERE codigo = @codigo;";

                        using (SqlCommand updateCommand = new SqlCommand(updateQuery, cn))
                        {
                            updateCommand.Parameters.AddWithValue("@status", statusAtualizado);
                            updateCommand.Parameters.AddWithValue("@codigo", processoUploadId);
                            updateCommand.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro no processo de upload: {ex.Message}");
                }
            }

            cn.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Erro: " + ex.Message);
        }
    }
}


//else if (storageType.Equals("BlackBlazeB2", StringComparison.OrdinalIgnoreCase))
//{
//  comandoCopy = $"rclone copy --ignore-times -v \"{caminhoOrigem}\" BlackBlazeB2:testeCloudBerrybr/ --log-file \"C:\\caminho_log\\rclone.log\"";
//}