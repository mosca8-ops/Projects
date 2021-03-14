using System;
using System.Collections;
using System.Collections.Generic;
using TXT.WEAVR.Common;
using TXT.WEAVR.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace TXT.WEAVR.Procedure
{
    [Serializable]
    public class OptionalImageType : Optional<Image.Type>
    {
        public static implicit operator OptionalImageType(Image.Type value)
        {
            return new OptionalImageType()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Image.Type(OptionalImageType optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalFillMethod : Optional<Image.FillMethod>
    {
        public static implicit operator OptionalFillMethod(Image.FillMethod value)
        {
            return new OptionalFillMethod()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Image.FillMethod(OptionalFillMethod optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalFontStyle : Optional<FontStyle>
    {
        public static implicit operator OptionalFontStyle(FontStyle value)
        {
            return new OptionalFontStyle()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator FontStyle(OptionalFontStyle optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalTextAnchor : Optional<TextAnchor>
    {
        public static implicit operator OptionalTextAnchor(TextAnchor value)
        {
            return new OptionalTextAnchor()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator TextAnchor(OptionalTextAnchor optional)
        {
            return optional.value;
        }
    }

    [Serializable]
    public class OptionalBillboard : Optional<Billboard>
    {
        public static implicit operator OptionalBillboard(Billboard value)
        {
            return new OptionalBillboard()
            {
                enabled = true,
                value = value
            };
        }

        public static implicit operator Billboard(OptionalBillboard optional)
        {
            return optional.value;
        }
    }
}
