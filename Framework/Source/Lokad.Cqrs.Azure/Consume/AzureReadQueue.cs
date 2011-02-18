#region (c) 2010 Lokad Open Source - New BSD License 

// Copyright (c) Lokad 2010, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;
using Lokad.Cqrs.Lmf;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
namespace Lokad.Cqrs.Queue
{
	public sealed class AzureReadQueue
	{
		readonly IMessageSerializer _serializer;
		readonly ILog _log;

		readonly CloudBlobContainer _cloudBlob;
		readonly CloudQueue _posionQueue;
		readonly CloudQueue _queue;
		readonly string _queueName;

		const int RetryCount = 4;

		public AzureReadQueue(
			CloudStorageAccount account,
			string queueName,
			ILogProvider provider,
			IMessageSerializer serializer)
		{
			var blobClient = account.CreateCloudBlobClient();
			blobClient.RetryPolicy = RetryPolicies.NoRetry();

			_cloudBlob = blobClient.GetContainerReference(queueName);

			var queueClient = account.CreateCloudQueueClient();
			queueClient.RetryPolicy = RetryPolicies.NoRetry();
			_queue = queueClient.GetQueueReference(queueName);
			_posionQueue = queueClient.GetQueueReference(queueName + "-poison");

			_log = provider.Get("Queue[" + queueName + "]");

			_queueName = queueName;
			_serializer = serializer;
		}

		
		public string Name
		{
			get { return _queueName; }
		}

		public void Init()
		{
			_queue.CreateIfNotExist();
			_posionQueue.CreateIfNotExist();
			_cloudBlob.CreateIfNotExist();
		}

		public GetMessageResult GetMessage()
		{
			CloudQueueMessage message;
			try
			{
				message = _queue.GetMessage();
			}
			catch (Exception ex)
			{
				return GetMessageResult.Error(ex);
			}

			if (null == message)
			{
				return GetMessageResult.Wait;
			}

			if (message.DequeueCount > RetryCount)
			{
				// we consider this to be poison
				_log.ErrorFormat("Moving message {0} to poison queue {1}", message.Id, _posionQueue.Name);
				// Move to poison
				_posionQueue.AddMessage(message);
				_queue.DeleteMessage(message);
				return GetMessageResult.Retry;
			}

			try
			{
				var m = MessageUtil.ReadMessage(message.AsBytes, _serializer, DownloadPackage);
				var unpacked = new AzureMessageContext(message, m);
				return GetMessageResult.Success(unpacked);
			}
			catch (StorageClientException ex)
			{
				_log.WarnFormat(ex, "Storage access problems for {0}", message.Id);
				return GetMessageResult.Retry;
			}
			catch (Exception ex)
			{
				_log.ErrorFormat(ex, "Failed to deserialize envelope {0}. Moving to poison", message.Id);
				// new poison details
				_posionQueue.AddMessage(message);
				_queue.DeleteMessage(message);
				return GetMessageResult.Retry;
			}
		}

		byte[] DownloadPackage(string reference)
		{
			var blob = _cloudBlob.GetBlobReference(reference);
			return blob.DownloadByteArray();
		}

		public void AckMessage(AzureMessageContext message)
		{
			if (message == null) throw new ArgumentNullException("message");

			
			_log.Debug(message);
			_queue.DeleteMessage(message.CloudMessage);
		}
	}

	public sealed class AzureMessageContext
	{
		public readonly CloudQueueMessage CloudMessage;
		public readonly MessageEnvelope Unpacked;

		public AzureMessageContext(CloudQueueMessage cloudMessage, MessageEnvelope unpacked)
		{
			CloudMessage = cloudMessage;
			Unpacked = unpacked;
		}
	}


}