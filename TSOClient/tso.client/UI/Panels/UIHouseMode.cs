﻿/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Utils;
using FSO.SimAntics.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Rendering.Framework.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.HIT;
using FSO.Client.UI.Model;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// House Panel. Works very similarly to Options Panel in that it uses sub-panels chosen by which button you press in this one.
    /// Requires a LotController for obvious reasons, like build/buy.
    /// </summary>
    public class UIHouseMode : UIDestroyablePanel
    {
        public Texture2D DividerImage { get; set; }

        public UIButton HouseInfoButton { get; set; }
        public UIButton StatisticsButton { get; set; }
        public UIButton RoommatesButton { get; set; }
        public UIButton LogButton { get; set; }
        public UIButton AdmitBanButton { get; set; }
        public UIButton EnvironmentButton { get; set; }
        public UIButton BillsButton { get; set; }
        public UIButton LotResizeButton { get; set; }

        private Dictionary<UIButton, int> BtnToMode;

        private UIContainer Panel;
        private int CurrentPanel;

        public UIImage Background;
        public UIImage Divider;

        public UILotControl LotControl;
        public UIHouseMode(UILotControl lotController)
        {
            var useSmall = true;  //(FSOEnvironment.UIZoomFactor > 1f || GlobalSettings.Default.GraphicsWidth < 1024);
            var script = this.RenderScript("housepanel.uis");

            Background = new UIImage(GetTexture(useSmall ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 9;
            Background.BlockInput();
            this.AddAt(0, Background);

            Divider = script.Create<UIImage>("Divider");
            Divider.Texture = DividerImage;
            this.Add(Divider);

            BtnToMode = new Dictionary<UIButton, int>()
            {
                { HouseInfoButton, 0 },
                { StatisticsButton, 1 },
                { RoommatesButton, 2 },
                { LogButton, 3 },
                { AdmitBanButton, 4 },
                { EnvironmentButton, 5 },
                { BillsButton, 6 },
                { LotResizeButton, 7 }
            };

            StatisticsButton.Disabled = true;
            BillsButton.Disabled = true;
            LogButton.Disabled = true;

            foreach (var btn in BtnToMode.Keys)
                btn.OnButtonClick += SetMode;

            CurrentPanel = -1;
            LotControl = lotController;
        }

        private void SetMode(Framework.UIElement button)
        {
            if (button == HouseInfoButton)
            {
                var controller = GameFacade.Screens.CurrentUIScreen.FindController<FSO.Client.Controllers.CoreGameScreenController>();
                if (controller != null)
                {
                    controller.ShowLotPage(controller.GetCurrentLotID());
                    return;
                }
            }

            var btn = (UIButton)button;
            int newPanel = -1;
            BtnToMode.TryGetValue(btn, out newPanel);

            foreach (var ui in BtnToMode.Keys)
                ui.Selected = false;

            if (CurrentPanel != -1)
            {
                if (Panel is UIBuildableAreaPanel)
                    ((UIBuildableAreaPanel)Panel)?.Dispose();
                this.Remove(Panel);
            }
            if (newPanel != CurrentPanel)
            {
                btn.Selected = true;
                switch (newPanel)
                {
                    case 1:
                        Panel = new UIStatsPanel(LotControl);
                        break;
                    case 2:
                        Panel = new UIRoommatesPanel(LotControl);
                        break;
                    case 3:
                        Panel = new UILogPanel(LotControl);
                        break;
                    case 4:
                        Panel = new UIAdmitBanPanel(LotControl);
                        break;
                    case 5:
                        Panel = new UIEnvPanel(LotControl);
                        Panel.X = 232;
                        Panel.Y = 0;
                        break;
                    case 7:
                        Panel = new UIBuildableAreaPanel(LotControl);
                        break;
                    default:
                        btn.Selected = false;
                        break;
                }
                if (Panel != null)
                {
                    if (newPanel != 5)
                    {
                        Panel.X = 225; //TODO: use uiscript positions
                        Panel.Y = 9;
                    }
                    this.Add(Panel);
                    CurrentPanel = newPanel;
                }
            }
            else
            {
                CurrentPanel = -1;
            }
        }

        public override void Destroy()
        {
            if (Panel is UIBuildableAreaPanel)
                ((UIBuildableAreaPanel)Panel)?.Dispose();
        }
    }

    // NOTE: See UIEnvPanel.cs for the EnvPanel and its subpanels.

    /// <summary>
    /// Same as TS1 house stats. TODO: make freeso calculate these values.
    /// </summary>
    public class UIStatsPanel : UIContainer
    {
        public UIStatsPanel(UILotControl lotController)
        {
            this.RenderScript("statisticspanel.uis");
        }
    }
    
    /// <summary>
    /// Set roommate build permissions. Check buttons disabled as anything but owner.
    /// </summary>
    public class UIRoommatesPanel : UIContainer
    {
        public UIRoommatesPanel(UILotControl lotController)
        {
            this.RenderScript("roommatespanel.uis");
        }
    }

    /// <summary>
    /// We already have the property log chat window. Maybe show that instead?
    /// </summary>
    public class UILogPanel : UIContainer
    {
        public UILogPanel(UILotControl lotController)
        {
            this.RenderScript("logpanel.uis");
        }
    }

    /// <summary>
    /// Fine-grained control of who can or can't enter the lot. TODO: list component
    /// </summary>
    public class UIAdmitBanPanel : UIContainer
    {
        private UIImage Background;
        public UIAdmitBanPanel(UILotControl lotController)
        {
            var script = this.RenderScript("admitbanpanel.uis");
            Background = script.Create<UIImage>("Background");
            this.AddAt(0, Background);
        }
    }

    /// <summary>
    /// Lets the owner upgrade or downgrade the property size. Modded a little for FreeSO to support floors.
    /// (TODO: move mods to UIScript mod instead of hardcoding them)
    /// </summary>
    public class UIBuildableAreaPanel : UIContainer
    {
        private UIImage BuildableAreaBackground;

        public UILabel LotSizeLabel { get; set; }
        public UILabel TilesLabel { get; set; }
        public UILabel UpgradeCostLabel { get; set; }
        public UILabel lackofRoomateLabel { get; set; }
        public UILabel TotalCostLabel { get; set; }
        public UILabel RoommatesLabel { get; set; }
        public UILabel SizeLevelLabel { get; set; }

        public UIButton LargerButton { get; set; }
        public UIButton SmallerButton { get; set; }
        public UIButton AcceptButton { get; set; }

        public UIButton FloorsLargerButton { get; set; }
        public UIButton FloorsSmallerButton { get; set; }

        private List<UILabel> Labels;
        private List<string> SavedInitialText = new List<string>();

        private UILotControl LotControl;
        private int UpdateSizeTarget = 0;
        private int UpdateFloorsTarget = 0;

        private RenderTarget2D PreviewTarget;
        private SpriteBatch Batch;
        private UIImage PreviewImage;

        private int OldLotSize;

        private Rectangle TargetSize;

        public UIBuildableAreaPanel(UILotControl lotController)
        {
            var script = this.RenderScript("buildableareapanel.uis");
            BuildableAreaBackground = script.Create<UIImage>("BuildableAreaBackground");
            this.AddAt(0, BuildableAreaBackground);

            Labels = new List<UILabel>()
            {
                LotSizeLabel,
                TilesLabel,
                UpgradeCostLabel,
                lackofRoomateLabel,
                TotalCostLabel,
                RoommatesLabel,
                SizeLevelLabel
            };

            foreach (var label in Labels)
            {
                label.Alignment = TextAlignment.Left;
                label.CaptionStyle = label.CaptionStyle.Clone();
                label.CaptionStyle.Shadow = true;
                label.Y -= 6;
                label.X += 3;
                SavedInitialText.Add(label.Caption.Replace("%d", "%s"));
            }
            LotControl = lotController;

            LargerButton.OnButtonClick += (btn) => { UpdateSizeTarget++; UpdateCost(); };
            SmallerButton.OnButtonClick += (btn) => { UpdateSizeTarget--; UpdateCost(); };
            AcceptButton.OnButtonClick += PurchaseLotSize;

            FloorsLargerButton = script.Create<UIButton>("LargerButton");
            FloorsLargerButton.Y += 39;
            FloorsLargerButton.OnButtonClick += (btn) => { UpdateFloorsTarget++; UpdateCost(); };
            Add(FloorsLargerButton);
            FloorsSmallerButton = script.Create<UIButton>("SmallerButton");
            FloorsSmallerButton.Y += 39;
            FloorsSmallerButton.OnButtonClick += (btn) => { UpdateFloorsTarget--; UpdateCost(); };
            Add(FloorsSmallerButton);

            var sizeLabel = new UILabel();
            sizeLabel.CaptionStyle = sizeLabel.CaptionStyle.Clone();
            sizeLabel.CaptionStyle.Shadow = true;
            sizeLabel.CaptionStyle.Size = 6;
            sizeLabel.Position = new Vector2(LargerButton.X+3, LargerButton.Y + 11);
            sizeLabel.Size = new Vector2(45, 0);
            sizeLabel.Alignment = TextAlignment.Center;
            sizeLabel.Caption = "Size";
            Add(sizeLabel);

            var floorsLabel = new UILabel();
            floorsLabel.CaptionStyle = sizeLabel.CaptionStyle.Clone();
            floorsLabel.CaptionStyle.Shadow = true;
            floorsLabel.CaptionStyle.Size = 6;
            floorsLabel.Position = new Vector2(FloorsLargerButton.X+3, FloorsLargerButton.Y + 11);
            floorsLabel.Size = new Vector2(45, 0);
            floorsLabel.Alignment = TextAlignment.Center;
            floorsLabel.Caption = "Floors";
            Add(floorsLabel);

            SizeLevelLabel.X -= 19;
            SizeLevelLabel.Alignment = TextAlignment.Center;
            SizeLevelLabel.Size = new Vector2(41, 1);

            PreviewTarget = new RenderTarget2D(GameFacade.GraphicsDevice, 72, 72, false, SurfaceFormat.Color, DepthFormat.None,
    (GlobalSettings.Default.AntiAlias) ? 4 : 0, RenderTargetUsage.PreserveContents);
            Batch = new SpriteBatch(GameFacade.GraphicsDevice);

            PreviewImage = new UIImage(PreviewTarget);
            PreviewImage.Position = BuildableAreaBackground.Position + new Vector2(2);
            Add(PreviewImage);

            UpdateCost();
        }

        private void PurchaseLotSize(UIElement button)
        {
            LotControl.vm.SendCommand(new VMNetChangeLotSizeCmd
            {
                LotSize = (byte)UpdateSizeTarget,
                LotStories = (byte)UpdateFloorsTarget
            });
            HITVM.Get().PlaySoundEvent(UISounds.ObjectPlace);
            AcceptButton.Disabled = true;
        }

        public void UpdateCost()
        {
            var lotInfo = LotControl.vm.TSOState;
            var lotSize = lotInfo.Size & 255;
            var lotFloors = (lotInfo.Size >> 8) & 255;
            var lotDir = lotInfo.Size >> 16;

            OldLotSize = lotInfo.Size;

            UpdateSizeTarget = Math.Min(Math.Max(lotSize, UpdateSizeTarget), VMBuildableAreaInfo.BuildableSizes.Length-1);
            UpdateFloorsTarget = Math.Min(Math.Max(lotFloors, UpdateFloorsTarget), 3);
            var totalTarget = UpdateFloorsTarget + UpdateSizeTarget;
            var totalOld = lotSize + lotFloors;

            AcceptButton.Disabled = (totalTarget == totalOld); //no upgrade selected

            var baseCost = VMBuildableAreaInfo.CalculateBaseCost(totalOld, totalTarget);
            var roomieCost = VMBuildableAreaInfo.CalculateRoomieCost(lotInfo.Roommates.Count, totalOld, totalTarget);

            if (baseCost + roomieCost > (LotControl.ActiveEntity?.TSOState.Budget.Value ?? 0)) AcceptButton.Disabled = true; //can't afford

            //TODO: read from uiscript
            TotalCostLabel.CaptionStyle.Color = (AcceptButton.Disabled)?new Color(255, 125, 125):TextStyle.DefaultLabel.Color;

            var targetTiles = VMBuildableAreaInfo.BuildableSizes[UpdateSizeTarget];

            string[][] applyText = new string[][]
            {
                new string[] { },
                new string[] { targetTiles.ToString(), targetTiles.ToString()+"x"+(UpdateFloorsTarget+2) },
                new string[] { baseCost.ToString() },
                new string[] { roomieCost.ToString() },
                new string[] { (baseCost+roomieCost).ToString() },
                new string[] { Math.Min(8, totalTarget+1).ToString() },
                new string[] { (UpdateSizeTarget+1) + "+" + UpdateFloorsTarget }
            };

            for (int i=0; i<Labels.Count; i++)
            {
                Labels[i].Caption = GetArgsString(SavedInitialText[i], applyText[i]);
            }
            TargetSize = LotControl.vm.Context.GetTSOBuildableArea(UpdateSizeTarget | (UpdateFloorsTarget << 8) | (lotDir << 16));
            RenderPreview(VMBuildableAreaInfo.BuildableSizes[lotSize], lotFloors + 2, targetTiles, UpdateFloorsTarget + 2);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (LotControl.vm.TSOState.Size != OldLotSize) UpdateCost();

            var blueprint = LotControl.vm.Context.Blueprint;
            if (blueprint.TargetBuildableArea != TargetSize)
            {
                blueprint.TargetBuildableArea = TargetSize;
                blueprint.Terrain.TerrainDirty = true;
                LotControl.World.InvalidateZoom();
            }
        }

        public void RenderPreview(int oldSize, int oldFloors, int newSize, int newFloors)
        {
            var gd = GameFacade.GraphicsDevice;
            gd.SetRenderTarget(PreviewTarget);
            gd.Clear(Color.TransparentBlack);
            Batch.Begin();

            //64x32 base lot with a 4px border from bottom, left and right edges.
            var baseCol = new Color(0x61, 0x80, 0x9F);
            var newCol = new Color(0xFF, 0xF7, 0x99);
            var oldCol = new Color(0xCC, 0xE8, 0xE2);
            var oldT = 2;
            var newT = 2;

            var shadO = new Vector2(1, 1); //shadow offset
            var left = new Vector2(4, (72 - 4) - 16);
            var bottom = new Vector2(72/2, (72 - 4));
            var right = new Vector2(72 - 4, (72 - 4) - 16);
            var top = new Vector2(72/2, (72 - 4) - 32);

            var ctrBase = (left + bottom) / 2;
            var ltc = (bottom - left)/2;

            DrawPath(Color.Black, Batch, 2, true, left + shadO, bottom + shadO, right + shadO, top + shadO);
            DrawPath(baseCol, Batch, 2, true, left, bottom, right, top);

            //draw new lot.
            float sizePct = newSize / 64f;
            var nLeft = ctrBase - (ltc) * sizePct;
            var nBtm = ctrBase + (ltc) * sizePct;
            var nRight = nBtm + (right - bottom) * sizePct;
            var nTop = nLeft + (top - left) * sizePct;

            if (newFloors > oldFloors || newSize > oldSize)
            {
                DrawLineStack(Color.Black, nTop+shadO, nRight+shadO, 5, newFloors, Batch, newT);
                DrawLineStack(newCol, nTop, nRight, 5, newFloors, Batch, newT);
            }

            if (newSize > oldSize)
            {
                DrawPath(Color.Black, Batch, newT, true, nLeft + shadO, nBtm + shadO, nRight + shadO, nTop + shadO);
                DrawPath(newCol, Batch, newT, true, nLeft, nBtm, nRight, nTop);
            }

            //draw old lot.
            sizePct = oldSize / 64f;
            var oLeft = ctrBase - (ltc) * sizePct;
            var oBtm = ctrBase + (ltc) * sizePct;
            var oRight = oBtm + (right - bottom) * sizePct;
            var oTop = oLeft + (top - left) * sizePct;

            DrawPath(Color.Black, Batch, oldT, true, oLeft + shadO, oBtm + shadO, oRight + shadO, oTop + shadO);
            DrawLineStack(Color.Black, oTop + shadO, oLeft + shadO, 5, oldFloors, Batch, oldT);
            DrawLineStack(oldCol, oTop, oLeft, 5, oldFloors, Batch, oldT);
            DrawPath(oldCol, Batch, oldT, true, oLeft, oBtm, oRight, oTop);

            //end
            Batch.End();
            gd.SetRenderTarget(null);
            PreviewImage.Texture = PreviewTarget;
        }

        public void Dispose()
        {
            PreviewTarget.Dispose();
            Batch.Dispose();
            var blueprint = LotControl.vm.Context.Blueprint;
            blueprint.TargetBuildableArea = new Rectangle();
            blueprint.Terrain.TerrainDirty = true;
            LotControl.World.InvalidateZoom();
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, BuildableAreaBackground.Texture, new Rectangle(138, 0, 46, 51), 
                BuildableAreaBackground.Position + new Vector2(138, 39), new Vector2(1));
            base.Draw(batch);
        }

        //TODO: move these to drawing utils?

        private void DrawPath(Color tint, SpriteBatch batch, int lineWidth, bool complete, params Vector2[] path)
        {
            for (int i=0; i<path.Length; i++)
            {
                if (i == path.Length - 1 && !complete) return;
                DrawLine(tint, path[i], path[(i + 1) % path.Length], batch, lineWidth);
            }
        }

        private void DrawLineStack(Color tint, Vector2 pt1, Vector2 pt2, float height, int levels, SpriteBatch spriteBatch, int lineWidth)
        {
            DrawLine(tint, pt1, pt1 - new Vector2(0, height * levels), spriteBatch, lineWidth);
            DrawLine(tint, pt2, pt2 - new Vector2(0, height * levels), spriteBatch, lineWidth);
            for (int i=1; i<=levels; i++)
            {
                DrawLine(tint, pt1 - new Vector2(0, height * (i)), pt2 - new Vector2(0, height * (i)), spriteBatch, lineWidth);
            }
        }

        private void DrawLine(Color tint, Vector2 Start, Vector2 End, SpriteBatch spriteBatch, int lineWidth) //draws a line from Start to End.
        {
            double length = Math.Sqrt(Math.Pow(End.X - Start.X, 2) + Math.Pow(End.Y - Start.Y, 2));
            float direction = (float)Math.Atan2(End.Y - Start.Y, End.X - Start.X);
            spriteBatch.Draw(TextureGenerator.GetPxWhite(spriteBatch.GraphicsDevice), new Rectangle((int)Start.X, (int)Start.Y - (int)(lineWidth / 2), (int)length, lineWidth), 
                null, tint, direction, new Vector2(0, 0.5f), SpriteEffects.None, 0); 
        }

        private string GetArgsString(string argsStr, string[] args)
        {
            StringBuilder SBuilder = new StringBuilder();
            int ArgsCounter = 0;

            for (int i = 0; i < argsStr.Length; i++)
            {
                string CurrentArg = argsStr.Substring(i, 1);

                if (CurrentArg.Contains("%"))
                {
                    if (ArgsCounter < args.Length)
                    {
                        SBuilder.Append(CurrentArg.Replace("%", args[ArgsCounter]));
                        ArgsCounter++;
                        i++; //Next, CurrentArg will be either s or d - skip it!
                    }
                }
                else
                    SBuilder.Append(CurrentArg);
            }

            return SBuilder.ToString();
        }
    }
}
