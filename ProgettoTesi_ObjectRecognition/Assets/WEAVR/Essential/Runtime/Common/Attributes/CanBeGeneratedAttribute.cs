namespace TXT.WEAVR.Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using TXT.WEAVR.Core;
    using UnityEngine;

    public enum Relationship
    {
        Child,
        Sibling,
        Unrelated,
        Parent,
        Self
    }

    public enum Ownership
    {
        Individual,
        GameObject,
        Global
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class CanBeGeneratedAttribute : WeavrAttribute
    {
        public Type CreatedType { get; private set; }
        public Ownership Ownership { get; private set; }
        public Relationship CreateAs { get; private set; }
        public string NameSourcePath { get; private set; }
        public string FallbackName { get; set; }
        public string Suffix { get; set; }

        /// <summary>
        /// Indicates that this property can be filled with a generated child object
        /// </summary>
        public CanBeGeneratedAttribute() {
            NameSourcePath = null;
            CreateAs = Relationship.Child;
            Ownership = Ownership.Individual;
        }

        /// <summary>
        /// Indicates that this property can be filled with a generated object
        /// </summary>
        /// <param name="typeToCreate">The type to be created</param>
        public CanBeGeneratedAttribute(Type typeToCreate) : this(null, Relationship.Child, typeToCreate, Ownership.Individual) { }

        /// <summary>
        /// Indicates that this property can be filled with a generated object
        /// </summary>
        /// <param name="sourceName">The name of the generated object, either taken from a property with specified path or the specified name itself</param>
        /// <param name="createAs">The relationship of the generated object with the owner of the property</param>
        /// <param name="typeToCreate">The type to be created</param>
        /// <param name="ownership">[Optional] Who can handle the generated object</param>
        public CanBeGeneratedAttribute(string sourceName, Relationship createAs, Type typeToCreate = null, Ownership ownership = Ownership.Individual) {
            NameSourcePath = sourceName;
            CreateAs = createAs;
            Ownership = ownership;
            CreatedType = typeToCreate;
        }

        /// <summary>
        /// Indicates that this property can be filled with a generated object
        /// </summary>
        /// <param name="sourceName">The name of the generated object, either taken from a property with specified path or the specified name itself</param>
        /// <param name="ownership">[Optional] Who can handle the generated object</param>
        public CanBeGeneratedAttribute(string createdName, Ownership ownership = Ownership.Individual) : this(createdName, Relationship.Child, null, ownership) { }

        /// <summary>
        /// Indicates that this property can be filled with a generated object
        /// </summary>
        /// <param name="createAs">The relationship of the generated object with the owner of the property</param>
        /// <param name="ownership">[Optional] Who can handle the generated object</param>
        public CanBeGeneratedAttribute(Relationship createAs, Ownership ownership = Ownership.Individual) : this(null, createAs, null, ownership) { }
    }
}