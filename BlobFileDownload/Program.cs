using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;


namespace BlobFileDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            // storageAccountの作成（接続情報の定義）
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            // Blob Create the blob client object.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Get a reference to a container to use for the sample code, and create it if it does not exist.
            // コンテナ名を指定してコンテナ情報を取得する。
            CloudBlobContainer container = blobClient.GetContainerReference("img");

            //// もし無かったら作る
            //// container.CreateIfNotExists();

            // アップロード処理
            //BlobUpload(container);

            // テキストアップロード
            //TextUpload(container);

            // ダウンロード処理
            //BlobDownload(container);

            // 一括ダウンロード
            BlobDownload_Batch(container);
        }

        ///----------------------------------------------------------------------------
        /// <summary>
        ///     ストレージアカウントのBLOBコンテナへファイルをアップロード </summary>
        /// <param name="container">
        ///     CloudBlobContainerオブジェクト</param>
        ///----------------------------------------------------------------------------
        static void BlobUpload(CloudBlobContainer container)
        {
            //アップロード後のファイル名を指定
            var aBlob = container.GetBlockBlobReference("IMG_2307_22.JPG");

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
            };

            // SASを取得
            string sasContainerToken = aBlob.GetSharedAccessSignature(sasConstraints);

            // StorageCredentialsを使用してCloudBlobContainerインスタンスを生成
            var auth = new StorageCredentials(sasContainerToken);
            var blob = new CloudBlockBlob(aBlob.Uri, auth);

            //アップロード処理
            //アップロードしたいローカルのファイルを指定
            using (var fileStream = System.IO.File.OpenRead(@"C:\Users\kyama\Pictures\IMG_2307.JPG"))
            {
                blob.UploadFromStream(fileStream);
            }
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        ///     ストレージアカウントのBLOBコンテナから指定のオブジェクトをダウンロードする </summary>
        /// <param name="container">
        ///     CloudBlobContainer オブジェクト</param>
        ///-------------------------------------------------------------------------------------
        static void BlobDownload(CloudBlobContainer container)
        {
            //ダウンロードするファイル名を指定
            CloudBlockBlob blockBlob_download = container.GetBlockBlobReference("IMG_2307.JPG");

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.Read
            };

            // SASを取得
            string sasContainerToken = blockBlob_download.GetSharedAccessSignature(sasConstraints);

            // StorageCredentialsを使用してCloudBlobContainerインスタンスを生成
            var auth = new StorageCredentials(sasContainerToken);
            var blob = new CloudBlockBlob(blockBlob_download.Uri, auth);

            //ダウンロード処理
            //ダウンロード後のパスとファイル名を指定。
            blob.DownloadToFile(@"C:\Users\kyama\Pictures\IMG_yazawa.JPG", System.IO.FileMode.Create);
        }

        ///-------------------------------------------------------------------------------------
        /// <summary>
        ///     ストレージアカウントのBLOBコンテナのテキストファイルの内容を書き換える </summary>
        /// <param name="container">
        ///     CloudBlobContainer オブジェクト</param>
        ///-------------------------------------------------------------------------------------
        static void TextUpload(CloudBlobContainer container)
        {
            // blob
            CloudBlockBlob blob = container.GetBlockBlobReference("a.txt");
            string txt = string.Empty;

            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(5),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.Create
            };

            // SASを取得
            string sasContainerToken = blob.GetSharedAccessSignature(sasConstraints);

            //// StorageCredentialsを使用してCloudBlobContainerインスタンスを生成
            //var auth = new StorageCredentials(sasContainerToken);
            //var blob = new CloudBlockBlob(aBlob.Uri, auth);

            //// StorageCredentialsを使用しないでCloudBlockBlobインスタンスを生成
            var blob2 = new CloudBlockBlob(new Uri(blob.Uri.AbsoluteUri + sasContainerToken));

            // 書き込み済みの内容を取得する
            if (blob2.Exists())
            {
                txt = blob2.DownloadText();
            }

            // 書き込み済みの内容に追加して書き込み
            blob2.UploadText(txt + "こんにちは " + DateTime.Now + "\n");

            var creds3 = new StorageCredentials(sasContainerToken);
            var container3 = new CloudBlobContainer(container.Uri, creds3);
            container3.GetBlockBlobReference("b.txt").UploadText("hello");
        }

        ///-----------------------------------------------------------------------------------
        /// <summary>
        ///     ストレージアカウントのBLOBコンテナのファイルを一括ダウンロードする </summary>
        /// <param name="container">
        ///     CloudBlobContainer オブジェクト</param>
        ///-----------------------------------------------------------------------------------
        static void BlobDownload_Batch(CloudBlobContainer container)
        {
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy()
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(1),
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Read
            };

            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            BlobContinuationToken blobToken = null;

            // StorageCredentialsを使用してCloudBlobContainerインスタンスを生成
            var auth = new StorageCredentials(sasContainerToken);
            var blClient = new CloudBlobContainer(container.Uri, auth);

            List<IListBlobItem> results = new List<IListBlobItem>();

            do
            {
                // 一度の問い合わせで返却されるリストは5,000件まで
                BlobResultSegment blobList = blClient.ListBlobsSegmented(blobToken);
                blobToken = blobList.ContinuationToken;
                results.AddRange(blobList.Results);
                // 継続Tokenがnullになったらリスト終了
            } while (blobToken != null);


            // BlobのURL一覧を返却
            var blobUriList = new List<string>();

            foreach (IListBlobItem item in results)
            {
                // Blob名を取得
                string name = ((Microsoft.WindowsAzure.Storage.Blob.CloudBlockBlob)(item)).Name;

                // Blob
                var blob = blClient.GetBlobReference(name);

                // ダウンロード ※ダウンロード後のパスとファイル名を指定
                blob.DownloadToFile(@"C:\Users\kyama\Pictures\blobdownload\" + name, System.IO.FileMode.Create);
            }
        }
    }
}
