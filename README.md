AzureMgtCmd
===========

A Command line tool that wraps Azure Management Libraries.  Currently there are only a few Azure management tasks enabled.  Uploading files to a blob storage container and managing cloud service deployments.

This is great for the use I intended it for which is promoting packages and deploying packages from a Jenkins build job.  I prefered using .NET management libraries over PowerShell where setting up credentials is less ideal for Jenkins jobs.

Usage
-----
```
upload-files --acount --key --container --path --filename
```
- `--account` Storage Account Name
- `--key` Access Key
- `--container` Container Name
- `--path` Path to file(s) you want to upload to blob storage
- `--filename` Can be a filename or filespec like `*.*` or `*.txt`

```
create-cs --subscriptionid --service --package-url --config-path
```
- `--subscriptionid` Azure Subscription Id
- `--service` Cloud Service Name
- `--package-url` Url to a cloud service package already stored in a Azure blob storage container
- `--config-path` Path to configuration file on local file system.

```
wait-csready --subscriptionid --service --slot
```
- `--subscriptionid` Azure Subscription Id
- `--service` Cloud Service Name
- `--slot` The slot you want to wait for to be ready.  Normally this would `Staging`.
```
swap-cs --subscriptionid --service
```
- `--subscriptionid` Azure Subscription Id
- `--service` Cloud Service Name

```
delete-cs --subscriptionid --service --slot
```
- `--subscriptionid` Azure Subscription Id
- `--service` Cloud Service Name
- `--slot` The deployment slot you want to delete.  Normally this would `Staging` which is the old `Production` after you swaped.

Notes
---
Ensure your Azure `.publishsettings` file is in the current directory.  You can get `.publishsettings` file by hitting <https://windows.azure.com/download/publishprofile.aspx> in your browser.


`--subscriptionid` Id of the Azure subscription you want to manage.  You can get it by cracking open the `.publishsettings` file and copy the Id attribute from the desired subscription node.



There is always room for improvement.
