/**
 * Copyright © Veeam Software Group GmbH.
 */

import { UserRole } from '@veeam-vspc/shared/vspc/models/web-ui';

import type { UserConfigData } from '@veeam-vspc/shared/vspc/core';

import { allRoutes } from '../routes';
import { RouteIds } from '../../enums';
import { AppPortalLayout } from '../../../views/components/AppPortalLayout';

export const resellerOwnerRoleConfig: UserConfigData<typeof UserRole.ResellerOwner, any> = {
    role: UserRole.ResellerOwner,
    routes: [
        { // External pages
            path: '',
            index: true,
            component: AppPortalLayout,
            items: [
                allRoutes[RouteIds.UsageReport],
                allRoutes[RouteIds.Dashboard],
                allRoutes[RouteIds.Settings],
            ],
        },
    ],
    sections: [],
};
