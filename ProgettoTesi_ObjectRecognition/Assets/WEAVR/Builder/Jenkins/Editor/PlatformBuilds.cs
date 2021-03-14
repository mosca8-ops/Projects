﻿using System;

namespace TXT.WEAVR.Builder
{
    public struct PlatformBuilds
    {
        public readonly Action PlatformBuildMethod;
        public readonly bool WillBuildPlatform;

        public PlatformBuilds(Action buildMethod, bool buildPlatform)
        {
            PlatformBuildMethod = buildMethod;
            WillBuildPlatform = buildPlatform;
        }
    }
}