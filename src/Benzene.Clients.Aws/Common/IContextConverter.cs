﻿namespace Benzene.Clients.Aws.Common;

public interface IContextConverter<TContextIn, TContextOut>
{
    public TContextOut CreateRequest(TContextIn contextIn);
    public void MapResponse(TContextIn contextIn, TContextOut contextOut);
}