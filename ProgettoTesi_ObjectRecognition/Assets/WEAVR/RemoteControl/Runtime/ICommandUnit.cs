using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TXT.WEAVR.RemoteControl
{
    public delegate void OnCommandFinishedDelegate(ICommandUnit unit, int commandId, object result);

    public interface ICommandUnit
    {
        void RegisterCommands(DataInterface dataInterface);
        void UnregisterCommands(DataInterface dataInterface);
    }

    public interface ICommand
    {
        string Name { get; }
        ICommandUnit Owner { get; }
        void Call(int id, object[] parameters, OnCommandFinishedDelegate onSuccess, OnCommandFinishedDelegate onFailure);
    }

    public interface IRawCommand
    {
        string CommandName { get; }
        void Execute(int id, ICommandData data, OnCommandFinishedDelegate onSuccess, OnCommandFinishedDelegate onFailure);
    }

    public interface ICommandData
    {
        int RequesterId { get; }
        IParameter[] Parameters { get; }
        object GetRawData();
        bool TryGet<T>(string key, out T data);
        bool TryGet<T>(out T data);
    }

    public interface ICommandDataPrototypeProvider
    {
        ICommandDataPrototype DataPrototype { get; }
    }

    public interface ICommandDataPrototype
    {
        ICommandData Create(int requestId, byte[] serializedData);
        ICommandData Create(int requestId, IParameter[] parameters);
    }

    public interface IDataParameter : IParameter
    {
        int Index { get; }
        string Data { get; }
    }
}
