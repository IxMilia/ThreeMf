﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using IxMilia.ThreeMf.Collections;
using IxMilia.ThreeMf.Extensions;

namespace IxMilia.ThreeMf
{
    public class ThreeMfBaseMaterials : ThreeMfResource, IThreeMfPropertyResource
    {
        public IList<ThreeMfBase> Bases { get; } = new ListNonNull<ThreeMfBase>();

        IEnumerable<IThreeMfPropertyItem> IThreeMfPropertyResource.PropertyItems => Bases;

        internal override XElement ToXElement(Dictionary<ThreeMfResource, int> resourceMap, Action<string, Stream> addArchiveEntry)
        {
            return new XElement(BaseMaterialsName,
                new XAttribute(IdAttributeName, Id),
                Bases.Select(b => b.ToXElement()));
        }

        internal static ThreeMfBaseMaterials ParseBaseMaterials(XElement element)
        {
            var baseMaterials = new ThreeMfBaseMaterials();
            baseMaterials.Id = element.AttributeIntValueOrThrow(IdAttributeName);
            foreach (var baseElement in element.Elements(ThreeMfBase.BaseName))
            {
                var baseMaterial = ThreeMfBase.ParseBaseMaterial(baseElement);
                baseMaterials.Bases.Add(baseMaterial);
            }

            return baseMaterials;
        }
    }
}
