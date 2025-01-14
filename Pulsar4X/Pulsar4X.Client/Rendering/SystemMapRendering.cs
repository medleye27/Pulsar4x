﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using ImGuiSDL2CS;
using SDL2;
using Pulsar4X.Orbital;
using Pulsar4X.Engine;
using Pulsar4X.Engine.Sensors;
using Pulsar4X.Messaging;
using Pulsar4X.JumpPoints;
using Pulsar4X.Names;
using Pulsar4X.Orbits;
using Pulsar4X.Ships;
using Pulsar4X.Weapons;
using Pulsar4X.Galaxy;
using Pulsar4X.Movement;

namespace Pulsar4X.SDL2UI
{
    internal class SystemMapRendering : UpdateWindowState
    {
        GlobalUIState _state;
        SystemSensorContacts? _sensorMgr;
        ConcurrentQueue<Message>? _sensorChanges;
        SystemState? _sysState;
        Camera _camera;
        internal IntPtr windowPtr;
        internal IntPtr surfacePtr;
        internal IntPtr rendererPtr;
        ImGuiSDL2CSWindow _window;
        internal Dictionary<string, IDrawData> UIWidgets = new ();
        ConcurrentDictionary<int, Icon> _testIcons = new ();
        ConcurrentDictionary<int, IDrawData> _entityIcons = new ();
        ConcurrentDictionary<int, IDrawData> _orbitRings = new ();
        ConcurrentDictionary<int, IDrawData> _moveIcons = new ();
        internal ConcurrentDictionary<int, NameIcon> _nameIcons = new ();

        internal List<IDrawData> SelectedEntityExtras = new List<IDrawData>();
        internal Vector2 GalacticMapPosition = new Vector2();
        //internal SystemMap_DrawableVM SysMap;
        Entity? _faction;

        internal SystemMapRendering(ImGuiSDL2CSWindow window, GlobalUIState state)
        {
            _state = state;

            _camera = _state.Camera;
            _window = window;
            windowPtr = window.Handle;
            surfacePtr = SDL.SDL_GetWindowSurface(windowPtr);
            rendererPtr = SDL.SDL_GetRenderer(windowPtr);
            //UIWidgets.Add(new CursorCrosshair(new Vector4())); //used for debugging the cursor world position.
            foreach (var item in TestDrawIconData.GetTestIcons())
            {
                _testIcons.TryAdd(-1, item);
            }

            //_state.OnStarSystemChanged += RespondToSystemChange;
            //_state.OnFactionChanged += RespondToSystemChange;
        }


        internal void Initialize(StarSystem starSys)
        {
            if (_state.StarSystemStates.ContainsKey(starSys.ID))
            {
                _sysState = _state.StarSystemStates[starSys.ID];
            }
            else
            {
                _sysState = new SystemState(starSys, _state.Faction.Id);
                _state.StarSystemStates[_sysState.StarSystem.ID] = _sysState;
            }

            _faction = _state.Faction;
            _sensorMgr = starSys.GetSensorContacts(_faction.Id);
            _sensorChanges = _sensorMgr.Changes.Subscribe();
            _sysState.OnEntityAdded += OnSystemStateEntityAdded;
            _sysState.OnEntityUpdated += OnSystemStateEntityUpdated;
            _sysState.OnEntityRemoved += OnSystemStateEntityRemoved;

            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }
        }

        public void UpdateSystemState(SystemState systemState)
        {
            _testIcons.Clear();
            _entityIcons.Clear();
            _orbitRings.Clear();
            _moveIcons.Clear();
            _nameIcons.Clear();

            _sysState = systemState;
            _state.StarSystemStates[_sysState.StarSystem.ID] = _sysState;

            _faction = _state.Faction;
            _sensorMgr = systemState.StarSystem.GetSensorContacts(_faction.Id);
            _sensorChanges = _sensorMgr.Changes.Subscribe();

            foreach (var entityItem in _sysState.EntityStatesWithPosition.Values)
            {
                AddIconable(entityItem);
            }
        }

        void AddIconable(EntityState entityState)
        {
            entityState.TryGetDataBlob<PositionDB>(out var positionDB);
            entityState.TryGetDataBlob<MassVolumeDB>(out var massVolumeDB);

            if (entityState.TryGetDataBlob<NameDB>(out var nameDB) && positionDB != null)
            {
                _nameIcons.TryAdd(entityState.Id, new NameIcon(entityState, nameDB, positionDB, _state));
            }

            if (entityState.TryGetDataBlob<OrbitDB>(out var orbitDB))
            {
                if (!orbitDB.IsStationary)
                {
                    OrbitIconBase orbit;
                    if (orbitDB.Eccentricity < 1)
                    {
                        orbit = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);
                        _orbitRings.TryAdd(entityState.Id, orbit);
                    }
                    else
                    {
                        orbit = new OrbitHyperbolicIcon2(entityState, _state.UserOrbitSettingsMtx);
                        _orbitRings.TryAdd(entityState.Id, orbit);
                    }
                }
            }

            if (entityState.TryGetDataBlob<NewtonMoveDB>(out var newtonMoveDB))
            {
                _orbitRings.TryAdd(entityState.Id, new NewtonMoveIcon(entityState, newtonMoveDB, _state.UserOrbitSettingsMtx));
            }

            if (entityState.TryGetDataBlob<NewtonSimpleMoveDB>(out var newtonSimpleMoveDB))
            {
                _orbitRings.TryAdd(entityState.Id, new NewtonSimpleIcon(entityState, newtonSimpleMoveDB, _state.UserOrbitSettingsMtx));
            }

            if (entityState.TryGetDataBlob<WarpMovingDB>(out var warpMovingDB) && positionDB != null)
            {
                _orbitRings.TryAdd(entityState.Id, new WarpMovingIcon(warpMovingDB, positionDB));
            }


            if (entityState.TryGetDataBlob<StarInfoDB>(out var starInfoDB)
                && massVolumeDB != null
                && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new StarIcon(starInfoDB, positionDB, massVolumeDB));
            }

            if (entityState.TryGetDataBlob<SystemBodyInfoDB>(out var systemBodyInfoDB)
                && massVolumeDB != null
                && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new SysBodyIcon(entityState, systemBodyInfoDB, positionDB, massVolumeDB));
            }

            if (entityState.TryGetDataBlob<ShipInfoDB>(out var shipInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new ShipIcon(entityState, shipInfoDB, positionDB));
            }

            if (entityState.TryGetDataBlob<ProjectileInfoDB>(out var projectileInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new ProjectileIcon(entityState, positionDB));
            }

            if (entityState.TryGetDataBlob<BeamInfoDB>(out var beamInfoDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new BeamIcon(beamInfoDB, positionDB));
            }

            if(entityState.TryGetDataBlob<JPSurveyableDB>(out var jPSurveyableDB) && positionDB != null)
            {
                _entityIcons.TryAdd(entityState.Id, new PointOfInterestIcon(positionDB));
            }

        }

        void RemoveIconable(int entityGuid)
        {
            _testIcons.TryRemove(entityGuid, out var testIcon);
            _entityIcons.TryRemove(entityGuid, out var entityIcon);
            _orbitRings.TryRemove(entityGuid, out var orbitIcon);
            _moveIcons.TryRemove(entityGuid, out var moveIcon);
            _nameIcons.TryRemove(entityGuid, out var nameIcon);
        }


        public void UpdateUserOrbitSettings()
        {
            foreach (var item in _orbitRings.Values)
            {
                if(item is IUpdateUserSettings foo)
                {
                    foo.UpdateUserSettings();
                }
            }
        }

        void HandleChanges(EntityState entityState)
        {

            foreach (var message in entityState.Changes)
            {
                if(message.EntityId == null) continue;

                if (message.MessageType == MessageTypes.DBAdded)
                {
                    if (message.DataBlob is OrbitDB)
                    {
                        OrbitDB orbitDB = (OrbitDB)message.DataBlob;
                        if (orbitDB.Parent == null)
                            continue;


                        if (!orbitDB.IsStationary)
                        {
                            if (_sysState != null && _sysState.EntityStatesWithPosition.ContainsKey(message.EntityId.Value))
                            {
                                entityState = _sysState.EntityStatesWithPosition[message.EntityId.Value];
                            }
                            else if(_sysState != null && message.FactionId != null && _sysState.StarSystem.TryGetEntityById(message.EntityId.Value, out var retrievedEntity))
                            {
                                entityState = new EntityState(retrievedEntity, message.EntityId.Value, message.FactionId.Value);
                            }

                            OrbitIconBase orbit;
                            if (orbitDB.Eccentricity < 1)
                            {
                               orbit = new OrbitEllipseIcon(entityState, _state.UserOrbitSettingsMtx);
                            }
                            else
                            {
                                orbit = new OrbitHyperbolicIcon2(entityState, _state.UserOrbitSettingsMtx);
                            }
                            _orbitRings[message.EntityId.Value] = orbit;

                        }
                    }
                    if (message.DataBlob is WarpMovingDB
                        && _sysState != null
                        && _sysState.StarSystem.TryGetEntityById(message.EntityId.Value, out var entity)
                        && entity.TryGetDatablob<PositionDB>(out var positionDB))
                    {
                        var widget = new WarpMovingIcon((WarpMovingDB)message.DataBlob, positionDB);
                        widget.OnPhysicsUpdate();
                        //Matrix matrix = new Matrix();
                        //matrix.Scale(_camera.ZoomLevel);
                        //widget.OnFrameUpdate(matrix, _camera);
                        _moveIcons[message.EntityId.Value] = widget;
                        //_moveIcons.Add(changeData.Entity.ID, widget);
                    }

                    if (message.DataBlob is NewtonMoveDB)
                    {

                        Icon orb = new NewtonMoveIcon(entityState, (NewtonMoveDB)message.DataBlob, _state.UserOrbitSettingsMtx);
                        _orbitRings.AddOrUpdate(message.EntityId.Value, orb, ((guid, data) => data = orb));
                    }
                    //if (changeData.Datablob is NameDB)
                    //TextIconList[changeData.Entity.ID] = new TextIcon(changeData.Entity, _camera);

                    //_entityIcons[changeData.Entity.ID] = new EntityIcon(changeData.Entity, _camera);
                }
                if (message.MessageType == MessageTypes.DBRemoved)
                {
                    if (message.DataBlob is OrbitDB)
                    {

                        _orbitRings.TryRemove(message.EntityId.Value, out var foo);
                    }
                    if (message.DataBlob is WarpMovingDB)
                    {
                        _moveIcons.TryRemove(message.EntityId.Value, out var foo);
                    }

                    if (message.DataBlob is NewtonMoveDB)
                    {
                        _orbitRings.TryRemove(message.EntityId.Value, out var foo);
                    }
                }
            }
        }

        void TextIconsDistribute()
        {
            if (_nameIcons.Count == 0)
                return;
            var occupiedPosition = new List<IRectangle>();
            IComparer<IRectangle> byViewPos = new ByViewPosition();
            var textIconList = new List<NameIcon>(_nameIcons.Values);


            //Consolidate TextIcons that share the same position and name
            textIconList.Sort();
            int listLength = textIconList.Count;
            int textIconQuantity = 1;
            for (int i = 1; i < listLength; i++)
            {
                if (textIconList[i - 1].CompareTo(textIconList[i]) == 0)
                {
                    textIconQuantity++;
                    textIconList.RemoveAt(i);
                    i--;
                    listLength--;
                }
                else if (textIconQuantity > 1)
                {
                    textIconList[i - 1].NameString += " x" + textIconQuantity;
                    textIconQuantity = 1;
                }
            }

            //Placement happens bottom to top, left to right
            //Each newly placed Texticon is compared to only the Texticons that are placed above its position
            //Therefore a sorted list of the occupied Positions is maintained
            occupiedPosition.Add(textIconList[0]);



            List<NameIcon> texiconsCopy = new List<NameIcon>();
            texiconsCopy.AddRange(_nameIcons.Values);

            int numTextIcons = texiconsCopy.Count;

            for (int i = 1; i < numTextIcons; i++)
            {
                var item = texiconsCopy[i - 1];
                Vector2 height = new Vector2() { X = 0, Y = item.Height };
                int lowestPosIndex = occupiedPosition.BinarySearch(item.ViewDisplayRect + height, byViewPos);
                int lpi = lowestPosIndex;
                if (lowestPosIndex < 0)
                    lpi = ~lowestPosIndex;

                for (int j = lpi; j < occupiedPosition.Count; j++)
                {
                    if (item.ViewDisplayRect.Intersects(occupiedPosition[j]))
                    {
                        var newpoint = new System.Numerics.Vector2()
                        {
                            X = item.ViewOffset.X,
                            Y = item.ViewOffset.Y - occupiedPosition[j].Height
                        };
                        item.ViewOffset = newpoint;
                    }
                }
                //Inserts the new label sorted
                int insertIndex = occupiedPosition.BinarySearch(item, byViewPos);
                if (insertIndex < 0) insertIndex = ~insertIndex;
                occupiedPosition.Insert(insertIndex, item);
            }


        }

        private void OnSystemStateEntityAdded(SystemState systemState, Entity entity)
        {
            if(systemState.EntityStatesWithPosition.ContainsKey(entity.Id))
                AddIconable(systemState.EntityStatesWithPosition[entity.Id]);
        }

        private void OnSystemStateEntityUpdated(SystemState systemState, int entityId, Message message)
        {
            // Refreseh the icons for the updated entity
            if(systemState.EntityStatesWithPosition.ContainsKey(entityId))
            {
                RemoveIconable(entityId);
                AddIconable(systemState.EntityStatesWithPosition[entityId]);
            }
        }

        private void OnSystemStateEntityRemoved(SystemState systemState, int entityId)
        {
            RemoveIconable(entityId);
        }

        internal void Draw()
        {

            if (_sysState != null)
            {
                foreach (var item in _sysState.EntityStatesWithPosition.Values)
                {
                    if (item.Changes.Count > 0)
                    {
                        HandleChanges(item);
                    }
                }
            }

            byte oR, oG, oB, oA;
            SDL.SDL_GetRenderDrawColor(rendererPtr, out oR, out oG, out oB, out oA);
            SDL.SDL_BlendMode blendMode;
            SDL.SDL_GetRenderDrawBlendMode(rendererPtr, out blendMode);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);

            var matrix = _camera.GetZoomMatrix();

            UpdateAndDraw(UIWidgets.Values.ToList(), matrix);

            UpdateAndDraw(_orbitRings.Values.ToList(), matrix);

            UpdateAndDraw(_moveIcons.Values.ToList(), matrix);

            UpdateAndDraw(_entityIcons.Values.ToList(), matrix);

            UpdateAndDraw(SelectedEntityExtras, matrix);


            //because _nameIcons are imgui not sdl, we don't draw them here.
            //we draw them in PulsarMainWindow.ImGuiLayout
            lock (_nameIcons)
            {
                foreach (var item in _nameIcons.Values)
                    item.OnFrameUpdate(matrix, _camera);
            }
            TextIconsDistribute();

            //ImGui.GetOverlayDrawList().AddText(new System.Numerics.Vector2(500, 500), 16777215, "FooBarBaz");

            SDL.SDL_SetRenderDrawColor(rendererPtr, oR, oG, oB, oA);
            SDL.SDL_SetRenderDrawBlendMode(rendererPtr, blendMode);
        }

        public void DrawNameIcons()
        {

            lock (_nameIcons)
            {
                List<NameIcon> nameIcons = new List<NameIcon>();
                foreach (var icon in _nameIcons.Values)
                {
                    if(SystemViewPreferences.GetInstance().ShouldDisplay("map", icon.EntityState.BodyType))
                        nameIcons.Add(icon);
                    //item.Draw(_uiState.rendererPtr, _uiState.Camera);
                }
                NameIcon.DrawAll(_state.rendererPtr, _state.Camera, nameIcons);
            }

        }

        void UpdateAndDraw(List<IDrawData> icons, Matrix matrix)
        {
            foreach (var item in icons)
                item.OnFrameUpdate(matrix, _camera);
            foreach (var item in icons)
                item.Draw(rendererPtr, _camera);
        }

        void UpdateAndDraw(IList<IDrawData> icons, Matrix matrix)
        {
            foreach (var item in icons)
                item.OnFrameUpdate(matrix, _camera);
            foreach (var item in icons)
                item.Draw(rendererPtr, _camera);
        }
        // void UpdateAndDraw(Dictionary<string, IDrawData> icons, Matrix matrix)
        // {
        //     lock (icons)
        //     {
        //         foreach (var item in icons.Values)
        //             item.OnFrameUpdate(matrix, _camera);
        //         foreach (var item in icons.Values)
        //             item.Draw(rendererPtr, _camera);
        //     }
        // }

        public override bool GetActive()
        {
            return true;
        }

        public override void OnGameTickChange(DateTime newDate)
        {

        }

        public override void OnSystemTickChange(DateTime newDate)
        {
            _state.PrimarySystemDateTime = newDate;

            foreach (var icon in UIWidgets.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _orbitRings.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _entityIcons.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _moveIcons.Values.ToArray())
            {
                icon.OnPhysicsUpdate();
            }
            foreach (var icon in _nameIcons.Values)
            {
                icon.OnPhysicsUpdate();
            }
            foreach(var icon in SelectedEntityExtras)
            {
                icon.OnPhysicsUpdate();
            }
        }
    }
}
