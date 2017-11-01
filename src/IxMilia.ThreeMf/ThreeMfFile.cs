﻿// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Xml.Linq;
using IxMilia.ThreeMf.Collections;

namespace IxMilia.ThreeMf
{
    public class ThreeMfFile
    {
        private const string RelationshipNamespace = "http://schemas.openxmlformats.org/package/2006/relationships";
        private const string ModelRelationshipType = "http://schemas.microsoft.com/3dmanufacturing/2013/01/3dmodel";
        private const string RelsEntryPath = "/_rels/.rels";
        private const string DefaultModelEntryName = "/3D/3dmodel";
        private const string ModelPathExtension = ".model";
        private const string TargetAttributeName = "Target";
        private const string IdAttributeName = "Id";
        private const string TypeAttributeName = "Type";

        internal static XName RelationshipsName = XName.Get("Relationships", RelationshipNamespace);
        private static XName RelationshipName = XName.Get("Relationship", RelationshipNamespace);

        public IList<ThreeMfModel> Models { get; } = new ListNonNull<ThreeMfModel>();

        public void Save(Stream stream)
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
            using (var archiveBuilder = new ThreeMfArchiveBuilder(archive))
            {
                var currentModelSuffix = 0;
                var nextModelFileName = new Func<string>(() =>
                {
                    var suffix = currentModelSuffix++ == 0 ? string.Empty : $"-{currentModelSuffix}";
                    return string.Concat(DefaultModelEntryName, suffix, ModelPathExtension);
                });
                var modelPaths = Enumerable.Range(0, Models.Count).Select(_ => nextModelFileName()).ToList();
                var rootRels = new XElement(RelationshipsName,
                    modelPaths.Select(path => GetRelationshipElement(path, archiveBuilder.NextRelationshipId(), ModelRelationshipType)));
                archiveBuilder.WriteXmlToArchive(RelsEntryPath, rootRels);

                for (int i = 0; i < Models.Count; i++)
                {
                    var model = Models[i];
                    var modelXml = model.ToXElement(archiveBuilder);
                    var modelPath = modelPaths[i];
                    archiveBuilder.WriteXmlToArchive(modelPath, modelXml);
                }
            }
        }

        internal static XElement GetRelationshipElement(string target, string id, string type)
        {
            return new XElement(RelationshipName,
                new XAttribute(TargetAttributeName, target),
                new XAttribute(IdAttributeName, id),
                new XAttribute(TypeAttributeName, type));
        }

        public static ThreeMfFile Load(Stream stream)
        {
            using (var package = Package.Open(stream))
            {
                return Load(package);
            }
        }

        public static ThreeMfFile Load(Package package)
        {
            var file = new ThreeMfFile();
            foreach (var modelRelationship in package.GetRelationshipsByType(ModelRelationshipType))
            {
                var modelUri = modelRelationship.TargetUri;
                var modelPart = package.GetPart(modelUri);
                using (var modelStream = modelPart.GetStream())
                {
                    var document = XDocument.Load(modelStream);
                    var model = ThreeMfModel.LoadXml(document.Root, package);
                    file.Models.Add(model);
                }
            }

            return file;
        }
    }
}
