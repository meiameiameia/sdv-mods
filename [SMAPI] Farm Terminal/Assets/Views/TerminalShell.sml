<frame layout="1260px 820px"
       background={@Mods/StardewUI/Sprites/MenuBackground}
       border={@Mods/StardewUI/Sprites/MenuBorder}
       border-thickness="36,36,40,36"
       padding="32,24,32,28">
    <lane layout="stretch stretch" orientation="vertical">
        <frame layout="stretch content"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="20,16,20,16">
            <lane layout="stretch content" orientation="vertical">
                <label font="dialogue" text={TitleText} />
                <label margin="0,8,0,0" text={SubtitleText} />
            </lane>
        </frame>

        <lane layout="stretch content" orientation="horizontal" margin="0,20,0,0">
            <lane layout="stretch content" orientation="horizontal">
                <lane *repeat={Tabs} layout="content content" orientation="horizontal" margin="0,0,12,0">
                    <button layout="150px content"
                            text={ButtonText}
                            click=|^OnTabActivated(Id)| />
                </lane>
            </lane>
            <button layout="180px content" margin="16,0,0,0" text="Refresh" click=|Refresh()| />
        </lane>

        <frame layout="stretch stretch"
               margin="0,20,0,0"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="24,18,24,24"
               *switch={ActiveModuleId}>
            <scrollable *case="Overview" layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label font="dialogue" text={ActiveSectionTitle} />
                    <label margin="0,12,0,0" text={OverviewSummaryText} />

                    <lane *repeat={OverviewCards} margin="0,18,0,0" orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,14,20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={:Title} />
                                <label margin="0,8,0,0" text={:Value} />
                                <label margin="0,8,0,0" text={:Detail} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </scrollable>

            <scrollable *case="Power" layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label font="dialogue" text={ActiveSectionTitle} />
                    <label *if={ShowPowerEmptyText} margin="0,12,0,0" text={PowerEmptyText} />

                    <lane *repeat={Networks} margin="0,18,0,0" orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,14,20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={:Title} />
                                <label margin="0,8,0,0" text={:LocationsText} />
                                <label margin="0,8,0,0" text={:TopologyText} />
                                <label margin="0,8,0,0" text={:FlowText} />
                                <label margin="0,8,0,0" text={:StorageText} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </scrollable>

            <scrollable *case="Consumers" layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label font="dialogue" text={ActiveSectionTitle} />
                    <label *if={ShowConsumersEmptyText} margin="0,12,0,0" text={ConsumersEmptyText} />

                    <lane *repeat={Consumers} margin="0,18,0,0" orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,14,20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={:DisplayName} />
                                <label margin="0,8,0,0" text={:StatusText} />
                                <label margin="0,8,0,0" text={:LocationText} />
                                <label margin="0,8,0,0" text={:DemandText} />
                                <label margin="0,8,0,0" text={:AllocationText} />
                                <label margin="0,8,0,0" text={:DetailText} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </scrollable>

            <scrollable *case="Sources" layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label font="dialogue" text={ActiveSectionTitle} />
                    <label *if={ShowSourcesEmptyText} margin="0,12,0,0" text={SourcesEmptyText} />

                    <lane *repeat={Sources} margin="0,18,0,0" orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,14,20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={:DisplayName} />
                                <label margin="0,8,0,0" text={:SourceType} />
                                <label margin="0,8,0,0" text={:StatusText} />
                                <label margin="0,8,0,0" text={:LocationText} />
                                <label margin="0,8,0,0" text={:PrimaryMetricText} />
                                <label margin="0,8,0,0" text={:SecondaryMetricText} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </scrollable>

            <scrollable *case="Alerts" layout="stretch stretch" peeking="48">
                <lane orientation="vertical">
                    <label font="dialogue" text={ActiveSectionTitle} />
                    <label *if={ShowAlertsEmptyText} margin="0,12,0,0" text={AlertsEmptyText} />

                    <lane *repeat={Alerts} margin="0,18,0,0" orientation="vertical">
                        <frame layout="stretch content"
                               background={@Mods/StardewUI/Sprites/ControlBorder}
                               padding="20,14,20,16">
                            <lane orientation="vertical">
                                <label font="dialogue" text={:Title} />
                                <label margin="0,8,0,0" text={:Severity} />
                                <label margin="0,8,0,0" text={:Detail} />
                            </lane>
                        </frame>
                    </lane>
                </lane>
            </scrollable>
        </frame>

        <frame layout="stretch content"
               margin="0,20,0,0"
               background={@Mods/StardewUI/Sprites/ControlBorder}
               padding="18,12,18,12">
            <lane layout="stretch content" orientation="horizontal">
                <label layout="stretch content" text={ScopeText} />
                <label margin="24,0,0,0" text={RefreshStatusText} />
            </lane>
        </frame>
    </lane>
</frame>
