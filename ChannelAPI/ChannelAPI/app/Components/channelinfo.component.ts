import { Component, Input, OnInit } from '@angular/core';
import * as _ from 'lodash';
import 'rxjs/add/operator/filter';
import 'rxjs/add/operator/finally'
import { ChannelService } from '../Service/channel.service';
import { IChannel } from '../Models/channel';

@Component({
    selector: 'channel-info',
    templateUrl: 'app/Components/channelinfo.component.html',
    styleUrls: ['app/Styles/channelinfo.component.css']
})

export class ChannelInfoComponent implements OnInit {

    @Input() set channel (value: IChannel){
        this._channel = value;
        if (this._activeRegions)
            this.loadRegions();
    } get channel(){ return this._channel; };
    _channel: IChannel;
    _activeRegions: string[];
    regions: {};
    
    constructor(private _channelService: ChannelService)
    {

    }

    ngOnInit() {
        this.getActiveRegions();
    }

    getActiveRegions() {
        console.log('getActiveRegions called');

        this._channelService.get('/api/region/active')
            .finally(() => { 
                this.loadRegions();
            })
            .subscribe(x => {
                this._activeRegions = x;
            });
    }

    loadRegions() {
        console.log('loadRegions called');
        if (!this._channel) { return; }

        this._channelService.getBy('/api/channel/', this._channel.strFIOSServiceId)
            .map(arr => arr
                .filter(ch => {
                    return this._activeRegions.includes(ch.strFIOSRegionName);
                })
                .map((ch: IChannel) => {
                    return { name: ch.strFIOSRegionName, channel: ch.intChannelPosition, genre: ch.strStationGenre };
                })
            )
            .subscribe(x => {
                this.regions = x;
            });
    }
}
