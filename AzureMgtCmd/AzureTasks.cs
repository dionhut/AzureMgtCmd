using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using System.Threading;
using System.IO;
using System.Net.Http;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureMgtCmd
{
    public class CloudServiceTasks
    {
        public static SubscriptionCloudCredentials GetCredentials(string subscriptionId, string base64EncodedCert)
        {
            return new CertificateCloudCredentials(subscriptionId, new X509Certificate2(Convert.FromBase64String(base64EncodedCert)));
        }

        public static void Swap(string subscriptionId, string base64EncodedCert, string serviceName)
        {
            ComputeManagementClient client = new ComputeManagementClient(GetCredentials(subscriptionId, base64EncodedCert));

            var production = client.Deployments.GetBySlot(serviceName, DeploymentSlot.Production);
            var staging = client.Deployments.GetBySlot(serviceName, DeploymentSlot.Staging);

            var res = client.Deployments.Swap(serviceName, new DeploymentSwapParameters()
            {
                ProductionDeployment = production.Name,
                SourceDeployment = staging.Name
            });

            var status = client.GetOperationStatus(res.RequestId);
            if (status.Status == OperationStatus.Failed)
            {
                throw new Exception(status.Error.Message);
            }
        }

        public static void Delete(string subscriptionId, string base64EncodedCert, string serviceName, string slotName)
        {
            ComputeManagementClient client = new ComputeManagementClient(GetCredentials(subscriptionId, base64EncodedCert));

            DeploymentSlot slot = GetDeploymentSlot(slotName);

            try
            {
                var res = client.Deployments.DeleteBySlot(serviceName, slot);

                var status = client.GetOperationStatus(res.RequestId);
                if (status.Status == OperationStatus.Failed)
                {
                    throw new Exception(status.Error.Message);
                }
            }
            catch (Exception)
            {
            }
        }

        public static void Create(string subscriptionId, string base64EncodedCert, string serviceName, string label, string packageUrl, string configPath)
        {
            ComputeManagementClient client = new ComputeManagementClient(GetCredentials(subscriptionId, base64EncodedCert));

            DeploymentSlot slot = DeploymentSlot.Staging;

            var res = client.Deployments.Create(serviceName, slot, new DeploymentCreateParameters()
            {
                Name = Guid.NewGuid().ToString(),
                Label = label,
                Configuration = File.ReadAllText(configPath),
                PackageUri = new Uri(packageUrl),
                StartDeployment = true
            });

            var status = client.GetOperationStatus(res.RequestId);
            if (status.Status == OperationStatus.Failed)
            {
                throw new Exception(status.Error.Message);
            }
        }

        public static void WaitForReady(string subscriptionId, string base64EncodedCert, string serviceName, string slotName, TimeSpan waitTime)
        {
            var task = Task.Factory.StartNew(() => WaitForReadyImpl(subscriptionId, base64EncodedCert, serviceName, slotName));

            if (!task.Wait(waitTime))
            {
                throw new TimeoutException(string.Format("The Task timed out in {0} minutes!", waitTime.TotalMinutes));
            }
        }

        public static void UploadFilesToBlobStorage(string accountName, string secret, string containerName, string path, string fileNames)
        {
            var account = new Microsoft.WindowsAzure.Storage.CloudStorageAccount(
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, secret), true);

            var client = account.CreateCloudBlobClient();

            var container = client.GetContainerReference(containerName);

            // Get the list of files
            var files = System.IO.Directory.EnumerateFiles(path, fileNames).ToList();

            if (files.Count == 0)
            {
                throw new Exception(string.Format("Failed to find file(s) {0} in {1}", fileNames, path));
            }

            foreach (var item in files)
            {
                var blockBlob = container.GetBlockBlobReference(System.IO.Path.GetFileName(item));

                // Create or overwrite the "myblob" blob with contents from a local file.
                using (var fileStream = System.IO.File.OpenRead(item))
                {
                    blockBlob.UploadFromStream(fileStream);
                }

                Console.WriteLine(string.Format("Successfully uploaded {0}", item));
            }
        }

        public static void DownloadBlob(string accountName, string secret, string containerName, string destPath, string fileName)
        {
            var account = new Microsoft.WindowsAzure.Storage.CloudStorageAccount(
                new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(accountName, secret), true);

            var client = account.CreateCloudBlobClient();

            var container = client.GetContainerReference(containerName);

            // Retrieve reference to a blob named "photo1.jpg".
            var blob = container.GetBlockBlobReference(fileName);

            var path = destPath + (destPath.Last() == '\\' ? "" : "\\" + fileName);

            // Save blob contents to a file.
            using (var fileStream = System.IO.File.OpenWrite(path))
            {
                blob.DownloadToStream(fileStream);
            }

            Console.WriteLine(string.Format("Successfully downloaded {0}", path));
        }

        public static string GetServiceUrl(string subscriptionId, string base64EncodedCert, string serviceName, string slotName)
        {
            ComputeManagementClient client = new ComputeManagementClient(GetCredentials(subscriptionId, base64EncodedCert));

            DeploymentSlot slot = GetDeploymentSlot(slotName);

            var res = client.Deployments.GetBySlot(serviceName, slot);

            return res.Uri.Host;
        }

        private static DeploymentSlot GetDeploymentSlot(string slotName)
        {
            switch (slotName)
            {
                case "Staging":
                    return DeploymentSlot.Staging;

                case "Production":
                    return DeploymentSlot.Production;

                default:
                    throw new Exception(string.Format("Invalid slotName: {0}", slotName));
            }
        }

        private static void WaitForReadyImpl(string subscriptionId, string base64EncodedCert, string serviceName, string slotName)
        {
            ComputeManagementClient client = new ComputeManagementClient(GetCredentials(subscriptionId, base64EncodedCert));

            DeploymentSlot slot = GetDeploymentSlot(slotName);

            var res = client.Deployments.GetBySlot(serviceName, slot);

            bool ready = false;
            while (!ready)
            {
                int readyInstances = 0;
                foreach (var item in res.RoleInstances)
                {
                    Console.WriteLine(string.Format("Instance name:{0} State:{1}", item.InstanceName, item.InstanceStatus));

                    if (item.InstanceStatus == "ReadyRole")
                    {
                        readyInstances++;
                    }

                    if (readyInstances >= res.RoleInstances.Count)
                    {
                        Console.WriteLine("All role instances ready!");
                        ready = true;
                    }
                }

                Thread.Sleep(5000);

                res = client.Deployments.GetBySlot(serviceName, slot);
            }

            // Warm up WebApp
            HttpClient http = new HttpClient();
            var response = http.GetAsync(res.Uri);
            while (!response.IsCompleted)
            {
                Console.WriteLine("Waiting for warm up to complete.");
                Thread.Sleep(5000);
            }
        }
    }
}