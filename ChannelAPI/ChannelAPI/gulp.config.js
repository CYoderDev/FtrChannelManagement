
module.exports = function () {

    var base = {
        webroot: "./wwwroot/",
        node_modules: "./node_modules/"
    };

    var config = {
        /**
            * Files paths
            */
        angular: base.node_modules + "@angular/**/*.js",
        app: "app/**/*.*",
        appDest: base.webroot + "app",
        channelRepository: "./ChannelLogoRepository/*.*",
        channelRepositoryDest: base.webroot + "/ChannelLogoRepository",
        js: base.webroot + "js/*.js",
        jsDest: base.webroot + 'js',
        css: base.webroot + "css/*.css",
        cssDest: base.webroot + 'css',
        lib: base.webroot + "lib/",
        node_modules: base.node_modules,
        angularWebApi: base.node_modules + "angular2-in-memory-web-api/*.js",
        corejs: base.node_modules + "core-js/client/shim*.js",
        zonejs: base.node_modules + "zone.js/dist/zone*.js",
        reflectjs: base.node_modules + "reflect-metadata/Reflect*.js",
        systemjs: base.node_modules + "systemjs/dist/*.js",
        rxjs: base.node_modules + "rxjs/**/*.js",
        rxjsMap: base.node_modules + "rxjs/**/*.map",
        jasminejs: base.node_modules + "jasmine-core/lib/jasmine-core/*.*",
        lodashjs: base.node_modules + "lodash/*.js",
        lodashdest: base.webroot + "lib/@types/lodash",
        systemconfigjs: "./systemjs.config.js",
        ng2bs3modaljs: base.node_modules + "ng2-bs3-modal/**/*.*",
        ng2bs3modaldest: base.webroot + "lib/ng2-bs3-modal",
        aggridangular: base.node_modules + "ag-grid-angular/**/*.js",
        aggrid: base.node_modules + "ag-grid/**/*.js",
        aggridstyle: base.node_modules + "ag-grid/dist/styles/*.*"
    };

    return config;
};