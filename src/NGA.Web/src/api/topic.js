import request from './request'

/**
 * 获取主题列表
 * @param {Object} params - 查询参数
 * @param {number} params.PageIndex - 页码
 * @param {number} params.PageSize - 每页数量
 * @param {string} params.SearchKey - 搜索关键词
 */
export function getTopicList(params) {
    return request({
        url: '/api/Topic',
        method: 'get',
        params
    })
}

/**
 * 获取主题详情和回复
 * @param {string} tid - 主题ID
 * @param {Object} params - 查询参数
 * @param {boolean} params.OnlyAuthor - 只看楼主
 * @param {boolean} params.OnlyImage - 只看图片
 * @param {number} params.PageIndex - 页码
 * @param {number} params.PageSize - 每页数量
 */
export function getTopicDetail(tid, params) {
    return request({
        url: `/api/Topic/${tid}`,
        method: 'get',
        params
    })
}
