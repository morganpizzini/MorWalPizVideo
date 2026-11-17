import {
  useId,
  useRef,
  type KeyboardEvent,
  type ReactNode,
} from "react";

export interface TabbedWorkspaceTab {
  key: string;
  label: ReactNode;
  panel: ReactNode;
  disabled?: boolean;
}

export interface TabbedWorkspaceProps {
  "aria-label": string;
  tabs: readonly TabbedWorkspaceTab[];
  selectedKey: string;
  onSelectedKeyChange: (key: string) => void;
  className?: string;
  tabListClassName?: string;
  tabClassName?: string;
  panelClassName?: string;
}

export function TabbedWorkspace({
  "aria-label": ariaLabel,
  tabs,
  selectedKey,
  onSelectedKeyChange,
  className,
  tabListClassName,
  tabClassName,
  panelClassName,
}: TabbedWorkspaceProps) {
  const instanceId = useId();
  const tabRefs = useRef<Array<HTMLButtonElement | null>>([]);
  const selectedTab = tabs.find((tab) => tab.key === selectedKey) ?? tabs[0];
  const selectedIndex = selectedTab ? tabs.indexOf(selectedTab) : -1;

  const moveFocus = (event: KeyboardEvent<HTMLButtonElement>) => {
    if (tabs.length === 0) return;
    const enabledIndexes = tabs
      .map((tab, index) => (tab.disabled ? -1 : index))
      .filter((index) => index >= 0);
    if (enabledIndexes.length === 0) return;

    const currentIndex = tabs.findIndex((tab) => tab.key === event.currentTarget.dataset.tabKey);
    const currentPosition = enabledIndexes.indexOf(currentIndex);
    let nextPosition: number | undefined;
    if (event.key === "ArrowRight") nextPosition = (currentPosition + 1) % enabledIndexes.length;
    if (event.key === "ArrowLeft") nextPosition = (currentPosition - 1 + enabledIndexes.length) % enabledIndexes.length;
    if (event.key === "Home") nextPosition = 0;
    if (event.key === "End") nextPosition = enabledIndexes.length - 1;
    if (nextPosition === undefined) return;

    event.preventDefault();
    const nextIndex = enabledIndexes[nextPosition];
    tabRefs.current[nextIndex]?.focus();
    const nextTab = tabs[nextIndex];
    if (nextTab) onSelectedKeyChange(nextTab.key);
  };

  return (
    <div className={`mwp-tabbed-workspace ${className ?? ""}`}>
      <div
        className={`mwp-tabbed-workspace__tablist ${tabListClassName ?? ""}`}
        role="tablist"
        aria-label={ariaLabel}
      >
        {tabs.map((tab, index) => {
          const tabId = `${instanceId}-tab-${index}`;
          const panelId = `${instanceId}-panel-${index}`;
          const selected = tab.key === selectedTab?.key;
          return (
            <button
              key={tab.key}
              ref={(element) => { tabRefs.current[index] = element; }}
              id={tabId}
              className={`mwp-tabbed-workspace__tab ${tabClassName ?? ""}`}
              type="button"
              role="tab"
              data-tab-key={tab.key}
              aria-selected={selected}
              aria-controls={panelId}
              tabIndex={selected ? 0 : -1}
              disabled={tab.disabled}
              onClick={() => onSelectedKeyChange(tab.key)}
              onKeyDown={moveFocus}
            >
              {tab.label}
            </button>
          );
        })}
      </div>
      {selectedTab ? (
        <div
          id={`${instanceId}-panel-${selectedIndex}`}
          className={`mwp-tabbed-workspace__panel ${panelClassName ?? ""}`}
          role="tabpanel"
          aria-labelledby={`${instanceId}-tab-${selectedIndex}`}
          tabIndex={0}
        >
          {selectedTab.panel}
        </div>
      ) : null}
    </div>
  );
}

export default TabbedWorkspace;
