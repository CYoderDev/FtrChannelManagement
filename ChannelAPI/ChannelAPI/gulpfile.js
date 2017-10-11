/// <binding BeforeBuild='tscompile, copy:app' />
/// 
"use strict";

var gulp = require('gulp');
var config = require('./gulp.config')();
var cleanCSS = require('gulp-clean-css');
var clean = require('gulp-clean');
var rename = require('gulp-rename');
var runSequence = require('run-sequence');
var $ = require('gulp-load-plugins')({ lazy: true });
var ts = require('gulp-typescript');
var tsProject = ts.createProject("tsconfig.json");

gulp.task("clean:js", function (cb) {
    //return $.rimraf('wwwroot/js/*.min.js', cb);
    return gulp.src('wwwroot/js/*.min.js', { read: false }).pipe(clean());
});

gulp.task("clean:css", function (cb) {
    //return $.rimraf('wwwroot/css/*.min.css', cb);
    return gulp.src('wwwroot/css/*.min.css', { read: false }).pipe(clean());
});

gulp.task('minify:css', function () {
    return gulp.src(config.css)
        .pipe(cleanCSS())
        .pipe(rename({
            suffix: '.min'
        }))
        .pipe(gulp.dest(config.cssDest));
});

gulp.task("clean", ["clean:js", "clean:css"]);
gulp.task('minify', ['minify:css']);

gulp.task("copy:angular", function () {

    return gulp.src(config.angular,
        { base: config.node_modules + "@angular/" })
        .pipe(gulp.dest(config.lib + "@angular/"));
});

gulp.task("copy:angularWebApi", function () {
    return gulp.src(config.angularWebApi,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:corejs", function () {
    return gulp.src(config.corejs,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:zonejs", function () {
    return gulp.src(config.zonejs,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:reflectjs", function () {
    return gulp.src(config.reflectjs,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:systemjs", function () {
    return gulp.src(config.systemjs,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:rxjs", function () {
    return gulp.src([config.rxjs,config.rxjsMap],
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:lodash", function () {
    return gulp.src(config.lodashjs,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:app", function () {
    return gulp.src(config.app)
        .pipe(gulp.dest(config.appDest));
});

gulp.task("copy:channelRepo", function () {
    return gulp.src(config.channelRepository)
        .pipe(gulp.dest(config.channelRepositoryDest));
});

gulp.task("copy:jasmine", function () {
    return gulp.src(config.jasminejs,
        { base: config.node_modules + "jasmine-core/lib" })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:configsysjs", function () {
    return gulp.src(config.systemconfigjs,
        { base: "."})
        .pipe(gulp.dest(config.jsDest));
});

gulp.task("copy:ng2",function() {
    return gulp.src(config.ng2bs3modaljs)
        .pipe(gulp.dest(config.ng2bs3modaldest));
});

gulp.task("copy:aggridangular", function () {
    return gulp.src(config.aggridangular,
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib));
});

gulp.task("copy:aggrid", function () {
    return gulp.src([config.aggrid,config.aggridstyle],
        { base: config.node_modules })
        .pipe(gulp.dest(config.lib))
})

gulp.task("dependencies", [
    "copy:angular",
    "copy:angularWebApi",
    "copy:corejs",
    "copy:zonejs",
    "copy:reflectjs",
    "copy:systemjs",
    "copy:rxjs",
    "copy:jasmine",
    "copy:configsysjs",
    "copy:ng2",
    "copy:app",
    "copy:lodash",
    "copy:aggridangular",
    "copy:aggrid",
    "copy:app"
]);

gulp.task("watch", function () {
    return $.watch(config.app)
        .pipe(gulp.dest(config.appDest));
});

gulp.task("tscompile", function () {
    return tsProject.src()
        .pipe(tsProject())
        .js.pipe(gulp.dest("."))
});

gulp.task("rebuild", function (callback) {
    runSequence('tscompile', 'copy:app');
})

gulp.task("default", function(callback) {
    runSequence('clean', 'minify', 'dependencies'), callback;
});