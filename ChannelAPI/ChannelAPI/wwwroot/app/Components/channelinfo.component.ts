import { Component, Input, OnInit } from '@angular/core';
import * as _ from 'lodash';
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
        this.loadRegions();
    } get channel(){ return this._channel; };
    _channel: IChannel;
    regions: {};
    
    constructor(private _channelService: ChannelService)
    {

    }

    ngOnInit() {

    }

    loadRegions() {
        console.log('loadRegions called');
        if (!this._channel) { return; }

        let channels: IChannel[];
        this._channelService.getBy('/api/channel/', this._channel.strFIOSServiceId)
            .map(arr => arr.map((ch: IChannel) =>
            {
                return { name: ch.strFIOSRegionName, channel: ch.intChannelPosition, genre: ch.strStationGenre };
            }))
            .subscribe(x => {
                this.regions = x;
            });
    }
}
