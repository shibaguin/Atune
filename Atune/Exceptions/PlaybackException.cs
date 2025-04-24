using System;

namespace Atune.Exceptions;

public class PlaybackException(string message) : Exception(message)
{
}
