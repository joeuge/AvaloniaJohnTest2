﻿namespace Caliburn.Micro
{
    public delegate Task AsyncEventHandler<TEventArgs>(
        object sender,
        TEventArgs e)
        where TEventArgs : EventArgs;
}
