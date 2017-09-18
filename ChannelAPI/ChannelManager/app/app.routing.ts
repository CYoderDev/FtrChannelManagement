import { ModuleWithProviders } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ChannelComponent } from './Components/channel.component';

const appRoutes: Routes = [
    { path: '', redirectTo: 'home', pathMatch: 'full' },
    { path: 'home', component: ChannelComponent }
];

export const routing: ModuleWithProviders = RouterModule.forRoot(appRoutes);