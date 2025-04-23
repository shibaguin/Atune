using System;

namespace Atune.Exceptions;

public class PlaybackException : Exception
{
    public PlaybackException(string message) : base(message) { }
} 
