﻿using Qwirkle.Core.CommonContext.ValueObjects;
using Qwirkle.Core.CommonContext;
using System.Collections.Generic;

namespace Qwirkle.Core.BoardContext.Entities
{
    public class Tile
    {
        public int Id { get; }
        public TileColor Color { get; }
        public TileForm Form { get; }
        public CoordinatesInBoard Coordinates { get; }

        public Tile(int id, TileColor color, TileForm form, CoordinatesInBoard coordinates)
        {
            Id = id;
            Color = color;
            Form = form;
            Coordinates = coordinates;
        }

        public Tile(TileColor color, TileForm form, CoordinatesInBoard coordinates) : this(0, color, form, coordinates)
        { }

        public Tile(int id)
        {
            Id = id;
        }
    }
}
