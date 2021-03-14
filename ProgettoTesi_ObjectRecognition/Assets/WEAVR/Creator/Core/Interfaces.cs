using System;
using System.Collections.Generic;

namespace TXT.WEAVR.Procedure
{

    public interface IDescriptorCatalogue
    {
        DescriptorGroup Root { get; }
    }


    public interface IAssetImporter
    {
        bool TryImport(List<Action> postImportCallback);
    }

    public interface ISmartCreatedCallback
    {
        void OnSmartCreated(bool byCloning);
    }

    public interface IPasteClient
    {
        void Paste(string serializedData);
    }
}
