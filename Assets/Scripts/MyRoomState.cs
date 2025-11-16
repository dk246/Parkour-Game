using Colyseus.Schema;
using System.Collections.Generic;

public class Player : Schema
{
    [Type(0, "number")]
    public float x = 0;

    [Type(1, "number")]
    public float y = 0;

    [Type(2, "number")]
    public float z = 0;


    [Type(3, "number")]
    public float rotY = 0;

    [Type(4, "number")]
    public float rotX = 0;

    [Type(5, "number")]
    public float rotZ = 0;

    [Type(6, "string")]
    public string name = "Player";
}

public class MyRoomState : Schema
{
    [Type(0, "map", typeof(MapSchema<Player>))]
    public MapSchema<Player> players = new MapSchema<Player>();
}