import { ModuleWithProviders } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ChannelComponent } from './Components/channel.component';

const appRoutes: Routes = [
    //{ path: 'home/ChannelLogoRepository', redirectTo: '/ChannelLogoRepository/', pathMatch: 'prefix' },
    { path: '', redirectTo: '/home', pathMatch: 'full' },
    //{ path: 'FtrChannelManager', pathMatch: 'prefix'},
    //{ path: '~/home', redirectTo: '~/home', pathMatch:'full'},
    { path: 'home', component: ChannelComponent }
];

export const routing: ModuleWithProviders = RouterModule.forRoot(appRoutes);