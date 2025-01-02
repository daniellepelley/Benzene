﻿using Benzene.Core.DI;

namespace Benzene.Aws.Sqs;

public class SqsRegistrations : RegistrationsBase
{
    public SqsRegistrations()
    {
        Add(".AddSqsConsumer()", x => x.AddSqsConsumer());
    }
}
