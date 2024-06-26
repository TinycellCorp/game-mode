﻿using System;
using Cysharp.Threading.Tasks;

namespace GameMode
{
    public enum GameModeState
    {
        Ended,
        Starting,
        Started,
        Ending
    }

    public interface IGameMode
    {
        GameModeState State { get; internal set; }
        
        UniTask OnStartAsync();
        UniTask OnEndAsync();
        void OnSwitchFailed(Exception exception);
    }
    
}