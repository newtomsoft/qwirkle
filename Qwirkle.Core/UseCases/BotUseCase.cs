﻿namespace Qwirkle.Core.UseCases;

public class BotUseCase
{
    private readonly InfoUseCase _infoUseCase;
    private readonly CoreUseCase _coreUseCase;
    private readonly IArtificialIntelligence _artificialIntelligence;

    public BotUseCase(InfoUseCase infoUseCase, CoreUseCase coreUseCase, IArtificialIntelligence artificialIntelligence)
    {
        _infoUseCase = infoUseCase;
        _coreUseCase = coreUseCase;
        _artificialIntelligence = artificialIntelligence;
    }

    public Game ToRename(Game game)
    {
        return null;
    }

    public List<PlayReturn> ComputeDoableMoves(int gameId, int userId)
    {
        var player = _infoUseCase.GetPlayer(gameId, userId);
        var board = _infoUseCase.GetGame(gameId).Board;
        _coreUseCase.ResetGame(player.GameId);
        var rack = player.Rack.WithoutDuplicatesTiles();

        var boardAdjoiningCoordinates = board.GetAdjoiningCoordinatesToTiles();

        var playReturnsWith1Tile = new List<PlayReturn>();
        foreach (var coordinates in boardAdjoiningCoordinates)
        {
            foreach (var tile in rack.Tiles)
            {
                var playReturn = TestPlayTiles(player, new List<TileOnBoard> { TileOnBoard.From(tile, coordinates) });
                if (playReturn.Code == PlayReturnCode.Ok) playReturnsWith1Tile.Add(playReturn);
            }
        }
        playReturnsWith1Tile = playReturnsWith1Tile.OrderByDescending(p => p.Points).ToList();

        var playReturnsWith2Tiles = new List<PlayReturn>();
        foreach (var playReturn in playReturnsWith1Tile)
        {
            var tilePlayed = playReturn.TilesPlayed[0];
            var currentTilesToTest = rack.Tiles.Where(t => t != tilePlayed).ToList();

            var firstGameMove = board.Tiles.Count == 0;
            if (firstGameMove)
            {
                playReturnsWith2Tiles.AddRange(ComputePlayReturnWith2TilesInRow(RandomRowType(), player, boardAdjoiningCoordinates, currentTilesToTest, tilePlayed, true));
            }
            else
            {
                foreach (RowType rowType in Enum.GetValues(typeof(RowType)))
                    playReturnsWith2Tiles.AddRange(ComputePlayReturnWith2TilesInRow(rowType, player, boardAdjoiningCoordinates, currentTilesToTest, tilePlayed, false));
            }
        }
        playReturnsWith2Tiles = playReturnsWith2Tiles.OrderBy(p => p.Points).ToList();


        var playReturnsWith3Tiles = new List<PlayReturn>();
        foreach (var playReturn in playReturnsWith2Tiles)
        {
            var firstTilePlayed = playReturn.TilesPlayed[0];
            var secondTilePlayed = playReturn.TilesPlayed[1];
            var rowType = firstTilePlayed.Coordinates.X == secondTilePlayed.Coordinates.X ? RowType.Column : RowType.Line;

            var currentTilesToTest = rack.Tiles.Where(t => t != firstTilePlayed && t != secondTilePlayed).ToList();
            playReturnsWith3Tiles.AddRange(ComputePlayReturnWith3TilesInRow(rowType, player, boardAdjoiningCoordinates, currentTilesToTest, firstTilePlayed, secondTilePlayed));
        }

        //we have all possible moves with 3 tiles :)
        var allPlayReturns = new List<PlayReturn>();
        allPlayReturns.AddRange(playReturnsWith3Tiles);
        allPlayReturns.AddRange(playReturnsWith2Tiles);
        allPlayReturns.AddRange(playReturnsWith1Tile);

        return allPlayReturns;


        static RowType RandomRowType()
        {
            var rowTypeValues = typeof(RowType).GetEnumValues();
            var index = new Random().Next(rowTypeValues.Length);
            return (RowType)rowTypeValues.GetValue(index)!;
        }
    }

    private IEnumerable<PlayReturn> ComputePlayReturnWith2TilesInRow(RowType rowType, Player player, IEnumerable<Coordinates> boardAdjoiningCoordinates, List<TileOnPlayer> tilesToTest, TileOnBoard firstTile, bool firstGameMove)
    {
        var (tilePlayedX, tilePlayedY) = firstTile.Coordinates;
        var coordinateChanging = rowType is RowType.Line ? tilePlayedX : tilePlayedY;
        var coordinateFixed = rowType is RowType.Line ? tilePlayedY : tilePlayedX;
        var playReturnsWith2Tiles = new List<PlayReturn>();
        var boardAdjoiningCoordinatesRow = rowType is RowType.Line ?
                                            boardAdjoiningCoordinates.Where(c => c.Y == coordinateFixed).Select(c => (int)c.X).ToList()
                                          : boardAdjoiningCoordinates.Where(c => c.X == coordinateFixed).Select(c => (int)c.Y).ToList();

        if (!firstGameMove)
        {
            if (coordinateChanging == boardAdjoiningCoordinatesRow.Max()) boardAdjoiningCoordinatesRow.Add(coordinateChanging + 1);
            if (coordinateChanging == boardAdjoiningCoordinatesRow.Min()) boardAdjoiningCoordinatesRow.Add(coordinateChanging - 1);
        }
        else
        {
            var addOrSubtract1Unit = new Random().Next(2) * 2 - 1;
            boardAdjoiningCoordinatesRow.Add(coordinateChanging + addOrSubtract1Unit);
        }

        boardAdjoiningCoordinatesRow.Remove(coordinateChanging);
        foreach (var currentCoordinate in boardAdjoiningCoordinatesRow)
        {
            foreach (var tile in tilesToTest)
            {
                var testedCoordinates = rowType is RowType.Line ? Coordinates.From(currentCoordinate, coordinateFixed) : Coordinates.From(coordinateFixed, currentCoordinate);
                var testedTile = TileOnBoard.From(tile, testedCoordinates);
                var playReturn2 = TestPlayTiles(player, new List<TileOnBoard> { firstTile, testedTile });
                if (playReturn2.Code == PlayReturnCode.Ok) playReturnsWith2Tiles.Add(playReturn2);
            }
        }
        return playReturnsWith2Tiles;
    }

    private IEnumerable<PlayReturn> ComputePlayReturnWith3TilesInRow(RowType rowType, Player player, IEnumerable<Coordinates> boardAdjoiningCoordinates, List<TileOnPlayer> rackTiles, TileOnBoard firstTile, TileOnBoard secondTile)
    {
        var (firstTilePlayedX, firstTilePlayedY) = firstTile.Coordinates;
        var (secondTilePlayedX, secondTilePlayedY) = secondTile.Coordinates;

        var coordinateChangingMax = rowType is RowType.Line ? Math.Max(firstTilePlayedX, secondTilePlayedX) : Math.Max(firstTilePlayedY, secondTilePlayedY);
        var coordinateChangingMin = rowType is RowType.Line ? Math.Min(firstTilePlayedX, secondTilePlayedX) : Math.Min(firstTilePlayedY, secondTilePlayedY);

        var coordinateFixed = rowType is RowType.Line ? firstTilePlayedY : firstTilePlayedX;
        var playReturnsWith3Tiles = new List<PlayReturn>();
        var boardAdjoiningCoordinatesRow = rowType is RowType.Line ?
            boardAdjoiningCoordinates.Where(c => c.Y == coordinateFixed).Select(c => (int)c.X).ToList()
            : boardAdjoiningCoordinates.Where(c => c.X == coordinateFixed).Select(c => (int)c.Y).ToList();

        if (coordinateChangingMax == boardAdjoiningCoordinatesRow.Max()) boardAdjoiningCoordinatesRow.Add(coordinateChangingMax + 1);
        if (coordinateChangingMin == boardAdjoiningCoordinatesRow.Min()) boardAdjoiningCoordinatesRow.Add(coordinateChangingMin - 1);
        boardAdjoiningCoordinatesRow.Remove(coordinateChangingMax);
        boardAdjoiningCoordinatesRow.Remove(coordinateChangingMin);

        foreach (var currentCoordinate in boardAdjoiningCoordinatesRow)
        {
            foreach (var tile in rackTiles)
            {
                var testedCoordinates = rowType is RowType.Line ? Coordinates.From(currentCoordinate, coordinateFixed) : Coordinates.From(coordinateFixed, currentCoordinate);
                var testedTile = TileOnBoard.From(tile, testedCoordinates);
                var playReturn2 = TestPlayTiles(player, new List<TileOnBoard> { firstTile, secondTile, testedTile });
                if (playReturn2.Code == PlayReturnCode.Ok) playReturnsWith3Tiles.Add(playReturn2);
            }
        }
        return playReturnsWith3Tiles;
    }

    private PlayReturn TestPlayTiles(Player player, List<TileOnBoard> tilesToPlay) => _coreUseCase.Play(tilesToPlay, player, true);
}