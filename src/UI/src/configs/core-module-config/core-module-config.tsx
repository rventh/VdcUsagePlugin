/**
 * Copyright © Veeam Software Group GmbH.
 */

import type { PluginCoreModuleConfig } from '@veeam-vspc/shared/vspc/core-modules';
import { enLocale } from '../languages';
import { getPortalHomeUrl } from '@veeam-vspc/shared/vspc/helpers';
import { CompanyPrefixes, VeeamVspcProductPrefixes } from '@veeam-vspc/shared/vspc/enums';
import { UserRole } from '@veeam-vspc/shared/vspc/models/web-ui';
import React from 'react';
import { ICON_SIZES, ProductLogoVSPC } from '@veeam-vspc/shared/icons';

export const coreModuleConfig: PluginCoreModuleConfig = {
    restPrefix: '/api/v3',
    baseUrl: '/uiapi',
    companyPrefix: CompanyPrefixes.VeeamVspc,
    productPrefix: 'MyPlugin' as VeeamVspcProductPrefixes,
    getLoginPath: (returnUrl?: string) => getPortalHomeUrl('/config/plugins')(returnUrl),
    configuratorConfig: {
        allRolesPaths: {
            [UserRole.PortalAdministrator]: () => (
                import('../user-roles/configs/portal-administrator.config').then(file => file.portalAdministratorRoleConfig)
            ),
            [UserRole.SiteAdministrator]: () => (
                import('../user-roles/configs/site-administrator.config').then(file => file.siteAdministratorRoleConfig)
            ),
            [UserRole.ResellerAdministrator]: () => (
                import('../user-roles/configs/reseller-administrator.config').then(file => file.resellerAdministratorRoleConfig)
            ),
            [UserRole.ResellerOwner]: () => (
                import('../user-roles/configs/reseller-owner.config').then(file => file.resellerOwnerRoleConfig)
            ),
            [UserRole.CompanyAdministrator]: () => (
                import('../user-roles/configs/company-administrator.config').then(file => file.companyAdministratorRoleConfig)
            ),
            [UserRole.CompanyOwner]: () => (
                import('../user-roles/configs/company-owner.config').then(file => file.companyOwnerRoleConfig)
            ),
        },
    },
    langConfig: {
        allLangPaths: {
            en: (): Promise<typeof enLocale> => import('../languages/locales/en.locale').then(file => file.enLocale),
        },
    },
    fileTransportConfig: {
        authorizationCodeProps: {
            queryKey: 'x-authorization-code',
        },
    },
    defaultAppData: {
        portalLogo: <ProductLogoVSPC size={ICON_SIZES.xl} />,
        fixedPortalName: 'VDC Usage',
        defaultPortalName: 'VDC Usage',
    },
    isDevPanelAllowedInDevMode: false,
};
