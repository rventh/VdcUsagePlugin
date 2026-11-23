/**
 * Copyright © Veeam Software Group GmbH.
 */

import React from 'react';
import { Icon, VspcAppRouteItem } from '@veeam-vspc/shared/components';
import { RouteLevels } from '@veeam-vspc/shared/vspc/stores';

import { RouteIds } from '../../enums';
import reportIconSrc from 'images/report.svg';
import dashboardIconSrc from 'images/dashboard.svg';
import settingsIconSrc from 'images/settings.svg';
import { UsageReportPage } from 'views/pages/UsageReportPage';
import { DashboardPage } from 'views/pages/DashboardPage';
import { SettingsPage } from 'views/pages/SettingsPage';

import type { MyPluginLang } from '../../languages';

export const UsageReportPageRoute = {
    path: '',
    exact: true,
    index: true,
    component: UsageReportPage,
    navIcon: () => <Icon src={reportIconSrc} />,
    level: RouteLevels.NavMenuItem,
    navNameKey: 'USAGE_REPORT' as MyPluginLang['USAGE_REPORT'],
};

export const DashboardPageRoute = {
    path: 'dashboard',
    exact: true,
    component: DashboardPage,
    navIcon: () => <Icon src={dashboardIconSrc} />,
    level: RouteLevels.NavMenuItem,
    navNameKey: 'DASHBOARD' as MyPluginLang['DASHBOARD'],
};

export const SettingsPageRoute = {
    path: 'settings',
    exact: true,
    component: SettingsPage,
    navIcon: () => <Icon src={settingsIconSrc} />,
    level: RouteLevels.NavMenuItem,
    navNameKey: 'SETTINGS' as MyPluginLang['SETTINGS'],
};

export const allRoutes: Record<RouteIds, VspcAppRouteItem> = {
    [RouteIds.UsageReport]: UsageReportPageRoute,
    [RouteIds.Dashboard]: DashboardPageRoute,
    [RouteIds.Settings]: SettingsPageRoute,
};
