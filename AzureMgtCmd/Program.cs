using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AzureMgtCmd
{
    class Program
    {
        private const int UPLOAD_FILES_ARG_CNT = 5;
        private const int DOWNLOAD_FILE_ARG_CNT = 5;
        private const int CREATE_CS_ARG_CNT = 5;
        private const int WAIT_CS_ARG_CNT = 3;
        private const int SWAP_CS_ARG_CNT = 2;
        private const int DELETE_CS_ARG_CNT = 3;
        private const int GET_CSURL_ARG_CNT = 3;

        static string GetArgValue(string[] args, string key)
        {
            string result = null;
            for (int i = 0; i < args.Length; i++)
            {
                if(args[i] == key)
                {
                    result = args[i + 1];
                    break;
                }
            }

            return result;
        }

        static int Main(string[] args)
        {
            if(args.Count() == 0 || args[0] == "help")
            {
                WriteUsage();
                return 0;
            }
            else if(args[0] == "upload-files" )
            {
                if (args.Count() != 1 + (UPLOAD_FILES_ARG_CNT * 2)) 
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {    
                    CloudServiceTasks.UploadFilesToBlobStorage(
                        GetArgValue(args, "--account"),
                        GetArgValue(args, "--key"),
                        GetArgValue(args, "--container"),
                        GetArgValue(args, "--path"),
                        GetArgValue(args, "--filename"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to upload file {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "download-file")
            {
                if (args.Count() != 1 + (DOWNLOAD_FILE_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    CloudServiceTasks.DownloadBlob(
                        GetArgValue(args, "--account"),
                        GetArgValue(args, "--key"),
                        GetArgValue(args, "--container"),
                        GetArgValue(args, "--dest-path"),
                        GetArgValue(args, "--filename"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to download file {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "create-cs")
            {
                if (args.Count() != 1 + (CREATE_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    CloudServiceTasks.Create(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--label"),
                        GetArgValue(args, "--package-url"),
                        GetArgValue(args, "--config-path"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to create cloud service {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "wait-csready")
            {
                if (args.Count() != 1 + (WAIT_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    CloudServiceTasks.WaitForReady(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--slot"),
                        GetArgValue(args, "--timeout"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to wait for cloud service ready {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "swap-cs")
            {
                if (args.Count() != 1 + (SWAP_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    CloudServiceTasks.Swap(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to swap cloud service {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "delete-cs")
            {
                if (args.Count() != 1 + (DELETE_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    CloudServiceTasks.Delete(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--slot"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to delete cloud service {0}", ex.Message));
                    return 1;
                }
            }
            else if (args[0] == "get-csurl")
            {
                if (args.Count() != 1 + (GET_CSURL_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return 1;
                }

                try
                {
                    Console.WriteLine(CloudServiceTasks.GetServiceUrl(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--slot")));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to get cloud service url {0}", ex.Message));
                    return 1;
                }
            }

            return 0;
        }

        static string GetSubscriptionCert(string subscriptionId)
        {
            string result = null;
            var files = System.IO.Directory.EnumerateFiles(".", "*.publishsettings").ToList();
            foreach (var item in files)
            {
                XElement xml = XElement.Load(item);
                foreach (XElement x in xml.Elements("PublishProfile").Elements("Subscription"))
                {
                    if (x.Attributes("Id").First().Value == subscriptionId)
                    {
                        result = x.Attributes("ManagementCertificate").First().Value;
                    }

                    break;
                }
            }

            if (result == null)
            {
                throw new ApplicationException(string.Format("cannot find certificate info from  *.publishsettings under folder:{0} for subscription id:{1}", System.IO.Directory.GetCurrentDirectory(), subscriptionId));
            }
            return result;
        }

        static void WriteUsage()
        {
            Console.WriteLine(string.Format("Azure Management Tool {0}", Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine(string.Format("usage:"));
            Console.WriteLine(string.Format("AzureMgtCmd upload-files --account --key --container --path --filename"));
            Console.WriteLine(string.Format("AzureMgtCmd download-file --account --key --container --dest-path --filename"));
            Console.WriteLine(string.Format("AzureMgtCmd create-cs --subscriptionid --service --label --package-url --config-path"));
            Console.WriteLine(string.Format("AzureMgtCmd wait-csready --subscriptionid --service --slot"));
            Console.WriteLine(string.Format("AzureMgtCmd swap-cs --subscriptionid --service"));
            Console.WriteLine(string.Format("AzureMgtCmd delete-cs --subscriptionid --service --slot"));
            Console.WriteLine(string.Format("AzureMgtCmd get-csurl --subscriptionid --service --slot"));
        }
    }
}
