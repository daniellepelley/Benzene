﻿using Benzene.Abstractions.Response;
using Benzene.Core.MessageHandlers;

namespace Benzene.Aws.Sns;

public class SnsMessageResultSetter : MessageResultSetterBase<SnsRecordContext>;