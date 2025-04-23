using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;

namespace Atune.Exceptions
{
    public class MediaPlayerInitializationException : Exception
    {
        public MediaPlayerInitializationException(string message) : base(message) {}
        public MediaPlayerInitializationException(string message, Exception inner) 
            : base(message, inner) {}
    }
} 
