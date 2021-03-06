﻿using System;
using System.Linq;
using System.Xml;
using Microsoft.Extensions.Configuration;
using ProjectUpgrade.Models;

namespace ProjectUpgrade
{
    public class ProjectBuilder
    {
        private readonly IConfigurationRoot _configuration;
        private readonly XmlDocument _newProject;
        private readonly XmlElement _root;
        private readonly ProjectModel _projectModel;

        public ProjectBuilder(ProjectModel projectModel, IConfigurationRoot configuration)
        {
            _configuration = configuration;

            _projectModel = projectModel;
            
            _newProject = new XmlDocument();
            _root = _newProject.CreateElement("Project");
            _root.SetAttribute("Sdk", "Microsoft.NET.Sdk");
            _newProject.AppendChild(_root);
        }

        public ProjectBuilder GenerateProjectReferenceSection()
        {
            if (!_projectModel.ProjectReferences.Any())
                return this;

            var dependencyItemGroup = _newProject.CreateElement("ItemGroup");
            _root.AppendChild(dependencyItemGroup);

            foreach (var reference in _projectModel.ProjectReferences)
            {
                var childNode = _newProject.CreateElement("ProjectReference");
                childNode.SetAttribute("Include", reference.RelativePath);
                dependencyItemGroup.AppendChild(childNode);
            }

            return this;
        }

        public ProjectBuilder GenerateDependenciesSection()
        {
            if (!_projectModel.ProjectDependencies.Any())
                return this;
            
            var dependencyItemGroup = _newProject.CreateElement("ItemGroup");

            foreach (var info in _projectModel.ProjectDependencies)
            {
                var childNode = _newProject.CreateElement("PackageReference");
                childNode.SetAttribute("Version", info.Version);
                childNode.SetAttribute("Include", info.PackageId);
                dependencyItemGroup.AppendChild(childNode);
            }
            
            _root.AppendChild(dependencyItemGroup);

            return this;
        }

        public ProjectBuilder GenerateCommonSection()
        {
            var generalPropGroup = _newProject.CreateElement("PropertyGroup");
            _root.AppendChild(generalPropGroup);

            AddNodeFromConfig(generalPropGroup, "targetFramework", true);
            AddNodeFromConfig(generalPropGroup, "copyright");
            AddNodeFromConfig(generalPropGroup, "company");
            AddNodeFromConfig(generalPropGroup, "authors");
            AddNodeFromConfig(generalPropGroup, "description");
            AddNodeFromConfig(generalPropGroup, "packageLicenseUrl");
            AddNodeFromConfig(generalPropGroup, "packageProjectUrl");
            AddNodeFromConfig(generalPropGroup, "packageIconUrl");
            AddNodeFromConfig(generalPropGroup, "repositoryUrl");
            AddNodeFromConfig(generalPropGroup, "repositoryType");
            AddNodeFromConfig(generalPropGroup, "packageTags");
            AddNodeFromConfig(generalPropGroup, "packageReleaseNotes");
            AddNodeFromConfig(generalPropGroup, "packageId");
            AddNodeFromConfig(generalPropGroup, "version");
            AddNodeFromConfig(generalPropGroup, "product");

            if (_projectModel.IsExecutable)
            {
                var childNode = _newProject.CreateElement("OutputType");
                childNode.InnerText = "Exe";
                generalPropGroup.AppendChild(childNode);
            }

            return this;
        }

        public XmlDocument Build()
        {
            return _newProject;
        }

        private void AddNodeFromConfig(XmlNode parentElement, string configKey, bool throwIfMissing = false)
        {
            var nodeName = CapitalizeFirstLetter(configKey);
            var configValue = _configuration[configKey];
            if (configValue != null)
            {
                var childNode = _newProject.CreateElement(nodeName);
                childNode.InnerText = configValue;
                parentElement.AppendChild(childNode);
            }
            else if (throwIfMissing)
            {
                throw new Exception($"Required parameter {configKey} is missing.");
            }
        }

        private static string CapitalizeFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            var charArray = s.ToCharArray();
            charArray[0] = char.ToUpper(charArray[0]);
            return new string(charArray);
        }
    }
}