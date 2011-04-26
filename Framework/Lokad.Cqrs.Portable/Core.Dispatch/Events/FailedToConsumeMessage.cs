﻿#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion

using System;

namespace Lokad.Cqrs.Core.Dispatch.Events
{
    public sealed class FailedToConsumeMessage : ISystemEvent
    {
        public Exception Exception { get; private set; }
        public string EnvelopeId { get; private set; }
        public string QueueName { get; private set; }

        public FailedToConsumeMessage(Exception exception, string envelopeId, string queueName)
        {
            Exception = exception;
            EnvelopeId = envelopeId;
            QueueName = queueName;
        }
    }

    public sealed class QuarantinedMessage : ISystemEvent
    {
        public Exception LastException { get; private set; }
        public string EnvelopeId { get; private set; }
        public string QueueName { get; private set; }

        public QuarantinedMessage(Exception lastException, string envelopeId, string queueName)
        {
            LastException = lastException;
            EnvelopeId = envelopeId;
            QueueName = queueName;
        }
    }
}