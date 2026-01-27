# NGA.Web - NGB前端

基于Vue 3和Vite构建的NGB前端应用。

## 技术栈

- Vue 3.4
- Vue Router 4
- Axios
- Vite 5

## 开发

```bash
# 安装依赖
npm install

# 启动开发服务器 (http://localhost:3000)
npm run dev

# 构建生产版本
npm run build

# 预览生产构建
npm run preview
```

## 环境配置

- **开发环境**: API地址为 `http://127.0.0.1:5000`
- **生产环境**: API地址为 `https://ngb.xiaofengyu.com`

## 功能

- ✅ 主题列表展示
- ✅ 主题详情展示
- ✅ 回复列表展示
- ✅ 分页功能
- ✅ 搜索功能
- ⏳ 用户登录（待实现）

## 项目结构

```
src/
├── api/              # API请求
├── components/       # 公共组件
├── router/           # 路由配置
├── views/            # 页面组件
├── App.vue           # 根组件
├── main.js           # 入口文件
└── style.css         # 全局样式
```
