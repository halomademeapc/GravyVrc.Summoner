using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace GravyVrc.Summoner.Windows.Helpers;

public class EnumCollectionExtension : MarkupExtension
{
    public Type EnumType { get; set; }

    protected override object ProvideValue(IXamlServiceProvider _)
    {
        if (EnumType != null)
        {
            return CreateEnumValueList(EnumType);
        }

        return default(object);
    }

    private List<object> CreateEnumValueList(Type enumType)
    {
        return Enum.GetNames(enumType)
            .Select(name => Enum.Parse(enumType, name))
            .ToList();
    }
}