﻿using System;
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
        private const int CREATE_CS_ARG_CNT = 4;
        private const int WAIT_CS_ARG_CNT = 3;
        private const int SWAP_CS_ARG_CNT = 2;
        private const int DELETE_CS_ARG_CNT = 3;

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

        static void Main(string[] args)
        {
            if(args.Count() == 0 || args[0] == "help")
            {
                WriteUsage();
                return;
            }
            else if(args[0] == "upload-files" )
            {
                if (args.Count() != 1 + (UPLOAD_FILES_ARG_CNT * 2)) 
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return;
                }

                try
                {    
                    CloudServiceTasks.UploadFilesToBlobStorage(
                        GetArgValue(args, "--acount"),
                        GetArgValue(args, "--key"),
                        GetArgValue(args, "--container"),
                        GetArgValue(args, "--path"),
                        GetArgValue(args, "--filename"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to upload file {0}", ex.Message));
                }
            }
            else if(args[0] == "create-cs")
            {
                if (args.Count() != 1 + (CREATE_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return;
                }

                try
                {
                    CloudServiceTasks.Create(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--package-url"),
                        GetArgValue(args, "--config-path"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to create cloud service {0}", ex.Message));
                }
            }
            else if (args[0] == "wait-csready")
            {
                if (args.Count() != 1 + (WAIT_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return;
                }

                try
                {
                    CloudServiceTasks.WaitForReady(
                        GetArgValue(args, "--subscriptionid"),
                        GetSubscriptionCert(GetArgValue(args, "--subscriptionid")),
                        GetArgValue(args, "--service"),
                        GetArgValue(args, "--slot"));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("Failed to wait for cloud service ready {0}", ex.Message));
                }
            }
            else if (args[0] == "swap-cs")
            {
                if (args.Count() != 1 + (SWAP_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return;
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
                }
            }
            else if (args[0] == "delete-cs")
            {
                if (args.Count() != 1 + (DELETE_CS_ARG_CNT * 2))
                {
                    Console.WriteLine("Invalid args");
                    WriteUsage();
                    return;
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
                }
            }
        }

        static string GetSubscriptionCert(string subscriptionId)
        {
            string result = null;
            var files = System.IO.Directory.EnumerateFiles(".", "*.publishsettings").ToList();
            foreach (var item in files)
            {
                Console.WriteLine(string.Format("publishsettings file {0}", item));

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

            return result;
        }

        static void WriteUsage()
        {
            Console.WriteLine(string.Format("Azure Management Tool {0}", Assembly.GetExecutingAssembly().GetName().Version));
            Console.WriteLine(string.Format("usage:"));
            Console.WriteLine(string.Format("AzureMgtCmd upload-files --acount --key --container --path --filename"));
            Console.WriteLine(string.Format("AzureMgtCmd create-cs --subscriptionid --service --package-url --config-path"));
            Console.WriteLine(string.Format("AzureMgtCmd wait-csready --subscriptionid --service --slot"));
            Console.WriteLine(string.Format("AzureMgtCmd swap-cs --subscriptionid --service"));
            Console.WriteLine(string.Format("AzureMgtCmd delete-cs --subscriptionid --service --slot"));
        }
    }
}