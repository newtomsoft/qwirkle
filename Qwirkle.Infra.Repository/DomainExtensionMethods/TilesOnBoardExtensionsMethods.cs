﻿namespace Qwirkle.Infra.Repository.DomainExtensionMethods;

public static class TilesOnBoardExtensionsMethods
{
    public static TileOnBoardDao ToTileOnBoardDao(this TileOnBoard tile, int gameId) => new() { TileId = tile.Id, GameId = gameId, PositionX = tile.Coordinates.X, PositionY = tile.Coordinates.Y };
    public static TileOnBagDao ToTileOnBagDao(this TileOnPlayerDao tileOnPlayer, int gameId) => new() { TileId = tileOnPlayer.TileId, GameId = gameId };
}
