#if DEBUG
//#define DEBUG_TIMERS
//#define DEBUG_SAVE_LOAD
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private void InitializeCampaign()
        {
            CreateShipyard();

            CreateInitialPlanetsAndBases();
            CreateInitialPopulation();

            UpdateHomeLocations();

            CalculateInitialProduction();
            CalculateBudget();

            JoinIrcServer();

            // confirms if we have a group of settings, a map, that will work for every 'playable' race

            ClassTypes minClassType = _minStartingClass[CurrentEra];
            ClassTypes maxClassType = _maxStartingClass[CurrentEra];

            for (int i = (int)Races.kFirstEmpire; i < (int)Races.kLastCartel; i++)
            {
                // checks the ships availability

                CreateShip((Races)i, minClassType, maxClassType, out Ship ship);

                _ships.Remove(ship.Id);

                // checks the homes availability

                if (_homeLocations[i][0] == null)
                    throw new NotSupportedException();
            }

            // restarts the campaign counters

            _smallTicks = 0;
            _seconds = 0;

            // tries to resume the first campaign savegame it finds on disk

#if DEBUG_SAVE_LOAD
            _savegameState = save2;
            _lastSavegame = _root + savegameDirectory + "testSaveAndLoadCampaign";

            long t0 = Environment.TickCount64;

            SaveCampaign(t0);
            LoadCampaign(t0);

            File.Delete(_lastSavegame + savegameExtension);

            _savegameState = 0;
            _lastSavegame = null;
#endif

            foreach (string savegame in Directory.EnumerateFiles(_root + savegameDirectory, "*" + savegameExtension, SearchOption.TopDirectoryOnly))
            {
                _lastSavegame = savegame[..^savegameExtension.Length];
                _savegameState = load2 + 1;

                break;
            }
        }

        private async Task ProcessCampaignAsync()
        {
            try
            {
                const long millisecondsPerSmallTick = 1000 / smallTicksPerSecond;

                List<int> listInt1 = [];
                List<int> listInt2 = [];
                Queue<int> queueInt = new();

#if DEBUG_TIMERS
                _cpuMovementDelay = 1;
                _cpuMovementMinRest = 1;
                _cpuMovementMaxRest = 2;

                _millisecondsPerTurn = 31_000;
#endif

                long lastSmallTick = 0;
                long lastSecondTick = 0;
                long lastTurnTick = -_millisecondsPerTurn;

                while (_isDisposing == 0L)
                {
                    // gets the current tick

                    long t0 = Environment.TickCount64;

                    // starting processes

                    ProcessCampaignTickBegin(t0);

                    // tries to process a small tick

                    if (t0 - lastSmallTick >= millisecondsPerSmallTick)
                    {
                        lastSmallTick = t0;

                        _smallTicks++;

                        ProcessCampaignSmallTick(t0, queueInt);
                    }

                    // tries to process a second

                    if (t0 - lastSecondTick >= 1_000)
                    {
                        lastSecondTick = t0;

                        _seconds++;

                        ProcessCampaignSecond(t0, listInt1, listInt2);

                        // tries to process a minute

                        if ((_seconds % 60) == 0)
                            ProcessCampaignMinute();
                    }

                    // tries to process a turn

                    if (t0 - lastTurnTick >= _millisecondsPerTurn)
                    {
                        lastTurnTick = t0;

                        _turn++;

                        ProcessCampaignTurn(queueInt);

                        // tries to process an year

                        if ((_turn % _turnsPerYear) == 0)
                            ProcessCampaignYear();
                    }

                    // ending processes

                    ProcessCampaignTickEnd(queueInt);

                    // waits a little

                    await Task.Delay(1);
                }
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                LogError(null, e);
            }
        }

        private void ProcessCampaignTickBegin(long t)
        {
            ProcessClientLogouts();
            ProcessClientMessages(t);
        }

        private void ProcessCampaignSmallTick(long t0, Queue<int> queueInt)
        {

#if DEBUG
            ProcessDrafts(queueInt);
#else
            try
            {
                ProcessDrafts(queueInt);
            }
            catch (Exception e)
            {
                LogError("ProcessDrafts()", e);
            }
#endif

#if DEBUG
            ProcessHumanMovements(t0, queueInt);
#else
            try
            {
                ProcessHumanMovements(t0, queueInt);
            }
            catch (Exception e)
            {
                LogError("ProcessHumanMovements()", e);
            }
#endif

        }

        private void ProcessCampaignSecond(long t0, List<int> listInt1, List<int> listInt2)
        {

#if DEBUG
            ProcessCpuMovements(t0, listInt1, listInt2);
#else
            try
            {
                ProcessCpuMovements(t0, listInt1, listInt2);
            }
            catch (Exception e)
            {
                LogError("ProcessCpuMovements()", e);
            }
#endif

#if DEBUG
            ProcessIO(t0);
#else
            try
            {
                ProcessIO(t0);
            }
            catch (Exception e)
            {
                LogError("ProcessIO()", e);
            }
#endif

#if DEBUG
            DebugMapPopulation();
#endif

        }

        private void ProcessCampaignMinute()
        {

#if DEBUG
            UpdateHexOwnership();
#else
            try
            {
                UpdateHexOwnership();
            }
            catch (Exception e)
            {
                LogError("UpdateHexOwnership()", e);
            }
#endif

#if DEBUG
            UpdateHomeLocations();
#else
            try
            {
                UpdateHomeLocations();
            }
            catch (Exception e)
            {
                LogError("UpdateHomeLocations()", e);
            }
#endif

#if DEBUG
            UpdateRaceList();
#else
            try
            {
                UpdateRaceList();
            }
            catch (Exception e)
            {
                LogError("UpdateRaceList()", e);
            }
#endif

        }

        private void ProcessCampaignTurn(Queue<int> queueInt)
        {

#if DEBUG
            ProcessBids(queueInt);
#else
            try
            {
                ProcessBids(queueInt);
            }
            catch (Exception e)
            {
                LogError("ProcessBids()", e);
            }
#endif

#if DEBUG
            CalculateMaintenance();
#else
            try
            {
                CalculateMaintenance();
            }
            catch (Exception e)
            {
                LogError("CalculateMaintenance()", e);
            }
#endif

#if DEBUG
            CalculateProduction();
#else
            try
            {
                CalculateProduction();
            }
            catch (Exception e)
            {
                LogError("CalculateProduction()", e);
            }
#endif

        }

        private void ProcessCampaignYear()
        {

#if DEBUG
            ClearShipyard();
#else
            try
            {
                ClearShipyard();
            }
            catch (Exception e)
            {
                LogError("ClearShipyard()", e);
            }
#endif

#if DEBUG
            CreateShipyard();
#else
            try
            {
                CreateShipyard();
            }
            catch (Exception e)
            {
                LogError("CreateShipyard()", e);
            }
#endif

#if DEBUG
            CalculateBudget();
#else
            try
            {
                CalculateBudget();
            }
            catch (Exception e)
            {
                LogError("CalculateBudget()", e);
            }
#endif

        }

        private void ProcessCampaignTickEnd(Queue<int> queueInt)
        {
            ProcessServerChat();
            ProcessClientRequests(queueInt);
        }

        private static void LogError(string source, Exception e)
        {
            StringBuilder t = new(2048);

            t.Append("ERROR: ");

            if (source != null)
            {
                Contract.Assert(source.Length > 0);

                t.Append(source);
                t.Append(" -> ");
            }

            t.Append(e.Message);
            t.AppendLine();
            t.Append(e.StackTrace);
            t.AppendLine();

            Console.WriteLine(t.ToString());
        }
    }
}
