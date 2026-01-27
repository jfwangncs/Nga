import { createRouter, createWebHistory } from 'vue-router'
import TopicList from '../views/TopicList.vue'
import TopicDetail from '../views/TopicDetail.vue'

const routes = [
    {
        path: '/',
        name: 'TopicList',
        component: TopicList
    },
    {
        path: '/topic/:tid',
        name: 'TopicDetail',
        component: TopicDetail,
        props: true
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
