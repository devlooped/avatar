﻿using System;

namespace Avatars
{
    /// <summary>
    /// Annotates a method that is a factory for avatars, so that a 
    /// compile-time or design-time generator can generate them ahead of time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AvatarGeneratorAttribute : Attribute
    {
    }
}