﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class monsterController : Character
{
    void Start()
    {
        Character.CharacterStats stats = new Character.CharacterStats(100);
        stats.team = Game.teamType.Monsters;

        this.init(stats);
        this.setName("MONSTER");
    }
}
