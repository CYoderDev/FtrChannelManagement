import { Component, Input, Output, EventEmitter, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import * as _ from 'lodash';
import 'rxjs/add/operator/filter';
import { ChannelService } from '../Service/channel.service';
import { IChannel } from '../Models/channel';
import { Logger } from '../Logging/default-logger.service';

@Component({
    selector: 'channel-info',
    templateUrl: 'app/Components/channelinfo.component.html',
    styleUrls: ['app/Styles/channelinfo.component.css']
})

export class ChannelInfoComponent implements OnInit {

    @Input() set channel(value: IChannel) {

        if (this._channel && this._channel.strFIOSServiceId != value.strFIOSServiceId)
            this.resetVars();
        else if (this._channel && this._channel.strFIOSServiceId == value.strFIOSServiceId
            && this._channel.dtCreateDate == value.dtCreateDate && this._channel.strStationCallSign == value.strStationCallSign
            && this._channel.strStationCallSign == value.strStationCallSign && this._channel.strStationDescription == value.strStationDescription)
            return;

        this._channel = value;
        if (this._activeRegions)
            this.loadRegions();
        this.loadStation();
    } get channel() { return this._channel; };

    @Output() stationchange: EventEmitter<string> = new EventEmitter<string>();

    @Output() onshow: EventEmitter<any> = new EventEmitter();

    _channel: IChannel;
    _station: any;
    _activeRegions: string[];
    regions: {};
    editField: string;
    showSubmit: boolean = false;
    isSubmitting: boolean = false;
    isSuccess: boolean;
    errorMsg: string;
    stationLoading: boolean = false;
    regionLoading: boolean = false;

    constructor(private _channelService: ChannelService, private logger: Logger) {
        this.editField = '';
    }

    ngOnInit() {
        this.getActiveRegions();
    }

    getActiveRegions() {
        this.logger.log('getActiveRegions called');

        this._channelService.get('/api/region/active')
            .subscribe(x => {
                this._activeRegions = x;
            }, (error) => {
                this.errorMsg = error;
            }, () => {
                this.loadRegions();
            });
    }

    loadRegions() {
        this.logger.log('loadRegions called');
        if (!this._channel) { return; }

        this.regionLoading = true;
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
                this.regions = _.uniqBy(x, function (reg: any) { return [reg.name, reg.channel, reg.genre].join()});
            }, (error) => {
                this.errorMsg = error;
            }, () => {
                this.regionLoading = false;
                if (!this.stationLoading)
                    setTimeout(() => { this.onshow.emit(); });
            });
    }

    loadStation() {
        this.logger.log('loadStation called');
        this.stationLoading = true;
        if (!this._channel) { return; }

        this._channelService.getBy('/api/station/', this._channel.strFIOSServiceId)
            .subscribe(x => {
                this._station = x;
            }, (error) => {
                this.errorMsg = error;
            }, () => {
                this.stationLoading = false;
                if (!this.regionLoading)
                    setTimeout(() => { this.onshow.emit(); });
            });
    }

    onEdit($event) {
        this.logger.log("onEdit event called");
        this.resetVars();
        var target = $event.target as HTMLElement;
        var title = target.title;

        this.editField = title;
    }

    onFieldChange($event) {
        this.logger.log("onFieldChange called");
        var newValue = $event.target.value;
        switch (this.editField)
        {
            case "station_name":
                {
                    if (this.channel.strStationName != newValue) {
                        this.showSubmit = true;
                        this._station.strStationName = newValue;
                    }
                    break;
                }
            case "station_desc":
                {
                    if (this.channel.strStationDescription != newValue) {
                        this.showSubmit = true;
                        this._station.strStationDescription = newValue;
                    }
                    break;
                }
            case "station_callsign":
                {
                    if (this.channel.strStationCallSign != newValue) {
                        this.showSubmit = true;
                        this._station.strStationCallSign = newValue;
                    }
                    break;
                }
        }
    }

    onFieldFocusOut() {
        this.logger.log("onFieldFocusOut called");
        this.editField = '';
    }

    onSubmit() {
        this.logger.log("onSubmit called");
        this.isSubmitting = true;
        if (!this._station) {
            this.errorMsg = 'Failed to load Fios Station.';
            this.showSubmit = false;
            return;
        }

        this._channelService.put('/api/station', this._station)
            .subscribe((resp) => {
                this.isSubmitting = false;
                this.isSuccess = true;
            }, (error) => {
                this.errorMsg = error
                this.isSuccess = false;
                this.isSubmitting = false;
            }, () => {
                this.loadStation();
                this.stationchange.emit(this._channel.strFIOSServiceId);
            });

        this.showSubmit = false;
    }

    onCancel($event) {
        this.logger.log("onCancel called");
        this.resetVars();
        this.showSubmit = false;
        this._station.strStationName = this._channel.strStationName;
        this._station.strStationCallSign = this._channel.strStationCallSign;
        this._station.strStationDescription = this._channel.strStationDescription;
    }

    resetVars() {
        this.editField = '';
        this.isSubmitting = false;
        this.errorMsg = undefined;
        this.isSuccess = false;
    }
}

@Component({
    selector: 'focusable-input',
    template: `
        <input #focusinput [(ngModel)]="model" value="{{inputValue}}" (input)="onFieldChange($event)" (focusout)="onFieldFocusOut($event)" autofocus/>
    `,
    styles: ['width:100%']
})

export class FocusableInput implements AfterViewInit {
    @ViewChild('focusinput') focusInput: ElementRef;
    @Input('model') model: any;
    @Input('inputValue') inputValue: string;

    @Output('onFieldChange') onFieldChangeEvent: EventEmitter<any> = new EventEmitter();
    @Output('onFieldFocusOut') onFieldFocusOutEvent: EventEmitter<MouseEvent> = new EventEmitter<MouseEvent>();

    ngAfterViewInit() {
        this.focusInput.nativeElement.focus();
    }

    onFieldChange($event) {
        this.onFieldChangeEvent.emit($event);
    }

    onFieldFocusOut($event) {
        this.onFieldFocusOutEvent.emit($event);
    }
}