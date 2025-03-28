//弹出提示
function showPopover(target, msg, direction) {
    direction = direction || 'right';
    target.attr("data-placement", direction);
    target.attr("data-content", msg);
    //$('[data-toggle="tooltip"]').tooltip();
    target.popover('show');
    target.focus();

    //2秒后消失提示框
    var id = setTimeout(
        function () {
            target.attr("data-content", "");
            target.popover('destroy');
        }, 2000
    );
}
//去掉无意义节点树
function simplify_tree(treedata) {
    treedata.forEach(function (x) {
        if (x.nodes.length < 1)
            delete x.nodes;
        else
            simplify_tree(x.nodes);
    });
}
//获取get参数
function getUrlParam(name) {//封装方法
    var reg = new RegExp("(^|&)" + name + "=([^&]*)(&|$)"); //构造一个含有目标参数的正则表达式对象
    var r = window.location.search.substr(1).match(reg); //匹配目标参数
    if (r !== null) return unescape(r[2]);
    return null; //返回参数值
}
//更新参数
function updateUrlParam(name, val, url) {
    var thisURL = url;
    if (!url)
        thisURL = document.location.href;
    // 如果 url中包含这个参数 则修改
    if (thisURL.indexOf(name + '=') > 0) {
        var v = getUrlParam(name);
        if (v !== null) {
            // 是否包含参数
            thisURL = thisURL.replace(name + '=' + v, name + '=' + val);
        }
        else {
            thisURL = thisURL.replace(name + '=', name + '=' + val);
        }

    } // 不包含这个参数 则添加
    else {
        if (thisURL.indexOf("?") > 0) {
            thisURL = thisURL + "&" + name + "=" + val;
        }
        else {
            thisURL = thisURL + "?" + name + "=" + val;
        }
    }
    return thisURL;
}
//对象转url字符串
function objectToQueryString(obj) {
    return Object.keys(obj).map(function (key) {
        return "".concat(encodeURIComponent(key), "=").concat(encodeURIComponent(obj[key]));
    }).join('&');
}
(function ($) {
    $.fn.extend({
        formdata: function () {
            var data = {};
            var t = $(this).serializeArray();
            $.each(t, function () {
                if (!data.hasOwnProperty(this.name))
                    data[this.name] = this.value;
                else
                    data[this.name] = data[this.name] + "," + this.value;
            });
            return data;
        },
        clearform: function () {
            $(this)[0].reset();
            $(this).find(".text-danger").text('');
            $(this).find('select').val(null).trigger('change');
        },
        initform: function (options) {
            //默认参数
            var defaults = {
                formdata: "",
                isDebug: false //是否需要调试，这个用于开发阶段，发布阶段请将设置为false，默认为false,true将会把name value打印出来
            };
            //如果传入的json字符串，将转为json对象
            var tempData = "";
            if ($.type(options) === "string") {
                defaults.formdata = JSON.parse(options);
            } else {
                defaults.formdata = options;
            }
            //设置参数
            // var setting = $.extend({}, defaults, tempData);
            var setting = defaults;
            var form = this;
            formdata = setting.formdata;
            //如果传入的json对象为空，则不做任何操作
            if (!$.isEmptyObject(formdata)) {
                var debugInfo = "";
                $.each(formdata, function (key, value) {
                    //是否开启调试，开启将会把name value打印出来
                    if (setting.isDebug) {
                        debugInfo += "name:" + key + "; value:" + value + "\r\n ";
                    }
                    //表单处理
                    var formField = form.find("[name='" + key + "']");
                    if ($.type(formField[0]) === "undefined") {
                        if (setting.isDebug) {
                            console.warn("can not find name:[" + key + "] in form!!!"); //没找到指定name的表单
                        }
                    } else {
                        var fieldTagName = formField[0].tagName.toLowerCase();
                        if (fieldTagName === "input") {
                            if (formField.attr("type") === "radio") {
                                $("input:radio[name='" + key + "'][value='" + value + "']").prop("checked", "checked");
                            } else if (formField.attr("type") === "checkbox") {
                                $("input:checkbox[name='" + key + "'][value='" + value + "']").prop("checked", "checked");
                            }
                            else {
                                formField.val(value);
                            }
                        } else if (fieldTagName === "label") {
                            formField.html(value);
                        } else if (fieldTagName === "select") {
                            if (!value)
                                formField.val("0").trigger('change');
                            else if ($.type(value) === "object")
                                formField.val(value.Id).trigger('change');
                            else if ($.type(value) === "number")
                                formField.val(value).trigger('change');
                            else if ($.type(value) === "array") {
                                var allid = "";
                                var selectfield = options[formField[0].name + "Selectfield"];
                                if (selectfield === "undefined")
                                    selectfield = "Id";
                                for (var i = 0; i < value.length; i++) {
                                    allid += (value[i][selectfield] + ",");
                                }
                                formField.val(allid.split(',')).trigger('change');
                            }
                            else if (value.indexOf(',') > 0)
                                formField.val(value.split(',')).trigger('change');
                            else {
                                formField.val(value).trigger('change');
                            }
                        }
                        else {
                            formField.val(value);
                        }
                    }
                    //图片链接处理form.find("img[fieldata=img_url]")
                    var formImage = form.find("img[fieldata=" + key + "]");
                    if ($.type(formImage[0]) !== "undefined") {
                        formImage.attr("src", value);
                    }
                    //a链接处理
                    var formLink = form.find("a[fieldata=" + key + "]");
                    if ($.type(formLink[0]) !== "undefined") {
                        formLink.attr("href", value);
                    }
                });
                if (setting.isDebug) {
                    console.log(debugInfo);
                }
            }
            return form; //返回对象，提供链式操作
        },
        disabledform: function () {
            $(this).find("select").attr("disabled", true);
            $(this).find("input").attr("disabled", true);
        },
        enabledform: function () {
            $(this).find("select").attr("disabled", false);
            $(this).find("input").attr("disabled", false);
        },
        cleardangertext: function () {
            $(this).find(".text-danger").text('');
        },
        submit: function (options) {
            var defaults = {
                clearform: true
            };
            options = $.extend({}, defaults, options);
            var para = $(this).formdata();
            var _self = this;
            $.ajax({
                type: 'POST',
                url: options.url,
                cache: false,
                data: para,
                success: function (data) {
                    var result = eval(data);
                    if (result.Code === 0) {
                        options.callback(data);
                        if (options.clearform)
                            $(_self).clearform();
                    } else {
                        $(_self).cleardangertext();
                        result.Errors.forEach(function (d) { $("#" + d.Key).siblings('.text-danger').text(d.Value); });
                        if (options.errorCallback)
                            options.errorCallback(data);
                    }
                }
            });

        },
        initselect2: function (opt) {
            //默认参数
            var defaults = {
                defaultnull: false,
                defaultvalue: "无"
            };
            opt = $.extend({}, defaults, opt);
            var _self = this;
            $.ajax({
                url: opt.url,
                data: opt.params,
                dataType: 'json',
                success: function (data) {
                    if (opt.defaultnull)
                        data.unshift({ id: 0, text: opt.defaultvalue });
                    $(_self).select2({
                        placeholder: "选择",
                        width: '100%',
                        language: 'zh-CN',
                        escapeMarkup: function (markup) { return markup; }, // 自定义格式化防止xss注入
                        data: data
                    });
                }
            });
        }
    });
})(jQuery);
