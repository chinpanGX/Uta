using System;
using System.Collections.Generic;
using MasterMemory;
using MasterMemory.Addons;
using MessagePack;

namespace Uta.MasterData
{
    public enum Plan
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Free")] Free = 1,
        [InputName("Sense")] Sense = 2,
        [InputName("Logic")] Logic = 3,
    }

    public enum CardType
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Active")] Active = 1,
        [InputName("Mental")] Mental = 2,
        [InputName("Trouble")] Trouble = 3,
    }

    public enum Rarity
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("N")] N = 1,
        [InputName("R")] R = 2,
        [InputName("SR")] SR = 3,
        [InputName("SSR")] SSR = 4,
    }

    public enum ParamKind
    {
        [InputName("Nothing")] Nothing = 0,
        [InputName("Singing")] Singing = 1,
        [InputName("Rhythm")] Rhythm = 2,
        [InputName("Expression")] Expression = 3,
    }
}
