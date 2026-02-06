<template>
  <div class="topic-detail-page">
    <AppHeader />

    <main class="main-content">
      <!-- 错误提示 -->
      <div v-if="errorMessage" class="error-toast">
        <span class="error-icon">⚠️</span>
        <span class="error-text">{{ errorMessage }}</span>
        <button class="error-close" @click="errorMessage = ''">×</button>
      </div>

      <div class="breadcrumb">
        <router-link :to="getListUrl()">首页</router-link>
        <span class="separator">/</span>
        <router-link :to="getListUrl()">话题列表</router-link>
        <span class="separator">/</span>
        <span class="current">详情</span>
      </div>

      <div v-if="topic" class="detail-wrapper">
        <!-- 主题卡片 -->
        <div class="topic-card">
          <div class="topic-header">
            <h1 class="title">{{ topic.title }}</h1>
            <div class="meta-row">
              <div class="author-section">
                <UserAvatar
                  :avatar="topic.avatar || getUserAvatar(topic.uid)"
                  :username="topic.userName || topic.uid || '匿名用户'"
                  size="medium"
                />
                <div class="author-info">
                  <span class="author">{{
                    topic.userName || topic.uid || "匿名用户"
                  }}</span>
                  <span class="time">{{ formatTime(topic.postDate) }}</span>
                </div>
              </div>
              <div class="stats-section">
                <span class="stat-item">
                  <svg
                    class="icon"
                    viewBox="0 0 1024 1024"
                    width="16"
                    height="16"
                  >
                    <path
                      d="M512 64C264.6 64 64 264.6 64 512s200.6 448 448 448 448-200.6 448-448S759.4 64 512 64z m0 820c-205.4 0-372-166.6-372-372s166.6-372 372-372 372 166.6 372 372-166.6 372-372 372z"
                      fill="currentColor"
                    />
                    <path
                      d="M464 336a48 48 0 1 0 96 0 48 48 0 1 0-96 0z m72 112h-48c-4.4 0-8 3.6-8 8v272c0 4.4 3.6 8 8 8h48c4.4 0 8-3.6 8-8V456c0-4.4-3.6-8-8-8z"
                      fill="currentColor"
                    />
                  </svg>
                  {{ replyCount }} 回复
                </span>
              </div>
            </div>
          </div>
          <div class="topic-body">
            <div class="content" v-html="processContent(mainContent)"></div>
          </div>
        </div>

        <!-- 回复列表 -->
        <div v-if="topic" class="replies-section">
          <div class="section-header">
            <h3>回复 ({{ replyCount }})</h3>
            <div class="filter-buttons">
              <button
                class="filter-btn"
                :class="{ active: onlyAuthor }"
                @click="toggleOnlyAuthor"
              >
                只看楼主
              </button>
              <button
                class="filter-btn"
                :class="{ active: onlyImage }"
                @click="toggleOnlyImage"
              >
                只看图片
              </button>
            </div>
          </div>

          <div v-if="filteredReplies.length > 0" class="reply-list">
            <div
              v-for="(reply, index) in displayedReplies"
              :key="reply.id"
              class="reply-item"
              :class="index % 2 === 0 ? 'reply-even' : 'reply-odd'"
            >
              <div class="reply-number">#{{ reply.sort }}</div>
              <UserAvatar
                :avatar="getUserAvatar(reply.uid)"
                :username="reply.uName || reply.uid || '匿名'"
                size="medium"
              />
              <div class="reply-content-wrapper">
                <div class="reply-header">
                  <span class="author">{{
                    reply.uName || reply.uid || "匿名"
                  }}</span>
                  <span class="time">{{ formatTime(reply.postDate) }}</span>
                </div>

                <!-- 引用回复 -->
                <div
                  v-if="reply.quotePid && getQuoteReply(reply.quotePid)"
                  class="quote-box"
                >
                  <div class="quote-header">
                    <span class="quote-icon">↳</span>
                    <UserAvatar
                      :avatar="
                        getQuoteUser(getQuoteReply(reply.quotePid)?.uid)
                          ?.avatar || ''
                      "
                      :username="
                        getQuoteUser(getQuoteReply(reply.quotePid)?.uid)
                          ?.userName || '用户'
                      "
                      size="small"
                    />
                    <span class="quote-author">{{
                      getQuoteUser(getQuoteReply(reply.quotePid)?.uid)
                        ?.userName ||
                      getQuoteReply(reply.quotePid)?.uName ||
                      "用户"
                    }}</span>
                    <span class="quote-floor"
                      >#{{ getQuoteReply(reply.quotePid)?.sort }}</span
                    >
                  </div>
                  <div class="quote-content">
                    {{
                      truncateContent(getQuoteReply(reply.quotePid)?.content)
                    }}
                  </div>
                </div>

                <div
                  class="reply-body"
                  v-html="processContent(reply.content)"
                ></div>
                <div class="reply-footer">
                  <span class="vote-item like">
                    <svg
                      viewBox="0 0 1024 1024"
                      width="16"
                      height="16"
                      fill="currentColor"
                    >
                      <path
                        d="M885.9 533.7c16.8-22.2 26.1-49.4 26.1-77.7 0-44.9-25.1-87.4-65.5-111.1a67.67 67.67 0 0 0-34.3-9.3H572.4l6-122.9c1.4-29.7-9.1-57.9-29.5-79.4A106.62 106.62 0 0 0 471 99.9c-52 0-98 35-111.8 85.1l-85.9 311H144c-17.7 0-32 14.3-32 32v364c0 17.7 14.3 32 32 32h601.3c9.2 0 18.2-1.8 26.5-5.4 47.6-20.3 78.3-66.8 78.3-118.4 0-12.6-1.8-25-5.4-37 16.8-22.2 26.1-49.4 26.1-77.7 0-12.6-1.8-25-5.4-37 16.8-22.2 26.1-49.4 26.1-77.7-.2-12.6-2-25.1-5.6-37.1zM184 852V568h81v284h-81z m636.4-353l-21.9 19 13.9 25.4a56.2 56.2 0 0 1 6.9 27.3c0 16.5-7.2 32.2-19.6 43l-21.9 19 13.9 25.4a56.2 56.2 0 0 1 6.9 27.3c0 16.5-7.2 32.2-19.6 43l-21.9 19 13.9 25.4a56.2 56.2 0 0 1 6.9 27.3c0 22.4-13.2 42.6-33.6 51.8H329V564.8l99.5-360.5a44.1 44.1 0 0 1 42.2-32.3c7.6 0 15.1 2.2 21.1 6.7 9.9 7.4 15.2 18.6 14.6 30.5l-9.6 198.4h314.4C829 418.5 840 436.9 840 456c0 16.5-7.2 32.1-19.6 43z"
                      />
                    </svg>
                    {{ reply.support || 0 }}
                  </span>
                  <span class="vote-item dislike">
                    <svg
                      viewBox="0 0 1024 1024"
                      width="16"
                      height="16"
                      fill="currentColor"
                    >
                      <path
                        d="M885.9 490.3c3.6-12 5.4-24.4 5.4-37 0-28.3-9.3-55.5-26.1-77.7 3.6-12 5.4-24.4 5.4-37 0-28.3-9.3-55.5-26.1-77.7 3.6-12 5.4-24.4 5.4-37 0-51.6-30.7-98.1-78.3-118.4a66.1 66.1 0 0 0-26.5-5.4H144c-17.7 0-32 14.3-32 32v364c0 17.7 14.3 32 32 32h129.3l85.8 310.8C372.9 889 418.9 924 470.9 924c29.7 0 57.4-11.8 77.9-33.4 20.5-21.5 31-49.7 29.5-79.4l-6-122.9h239.9c12.1 0 23.9-3.2 34.3-9.3 40.4-23.5 65.5-66.1 65.5-111 0-28.3-9.3-55.5-26.1-77.7zM184 456V172h81v284h-81z m627.2 160.4H496.8l9.6 198.4c.6 11.9-4.7 23.1-14.6 30.5-6.1 4.5-13.6 6.8-21.1 6.7a44.28 44.28 0 0 1-42.2-32.3L329 459.2V172h415.4a56.85 56.85 0 0 1 33.6 51.8c0 9.7-2.3 18.9-6.9 27.3l-13.9 25.4 21.9 19a56.76 56.76 0 0 1 19.6 43c0 9.7-2.3 18.9-6.9 27.3l-13.9 25.4 21.9 19a56.76 56.76 0 0 1 19.6 43c0 9.7-2.3 18.9-6.9 27.3l-13.9 25.4 21.9 19a56.76 56.76 0 0 1 19.6 43c0 19.1-11 37.5-28.8 48.4z"
                      />
                    </svg>
                    {{ reply.oppose || 0 }}
                  </span>
                </div>
              </div>
            </div>
          </div>

          <!-- 空状态提示 -->
          <div v-else class="empty-state">
            <div class="empty-icon">📭</div>
            <div class="empty-text">暂无符合条件的回复</div>
            <div class="empty-hint">试试调整筛选条件</div>
          </div>
        </div>
      </div>

      <!-- 分页 -->
      <div v-if="totalPages > 1" class="pagination">
        <button
          class="page-btn"
          :disabled="pageIndex <= 1"
          @click="changePage(pageIndex - 1)"
        >
          上一页
        </button>

        <template v-for="page in pageNumbers" :key="page">
          <button
            v-if="page !== '...'"
            class="page-num"
            :class="{ active: pageIndex === page }"
            @click="changePage(page)"
          >
            {{ page }}
          </button>
          <span v-else class="ellipsis">...</span>
        </template>

        <button
          class="page-btn"
          :disabled="pageIndex >= totalPages"
          @click="changePage(pageIndex + 1, true)"
        >
          下一页
        </button>
      </div>
    </main>

    <AppFooter />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { getTopicDetail } from "@/api/topic";
import AppHeader from "@/components/AppHeader.vue";
import AppFooter from "@/components/AppFooter.vue";
import UserAvatar from "@/components/UserAvatar.vue";

const route = useRoute();
const router = useRouter();
const tid = ref(route.params.tid);

const topic = ref(null);
const replies = ref([]);
const users = ref({});
const quoteReplies = ref({});
const quoteUsers = ref({});
const pageIndex = ref(Number(route.query.page) || 1);
const pageSize = ref(20);
const replyCount = ref(0);
const onlyAuthor = ref(route.query.onlyAuthor === "true");
const onlyImage = ref(route.query.onlyImage === "true");
const errorMessage = ref("");
const shouldAppend = ref(false); // 追踪是否应该追加加载

const totalPages = computed(() => {
  return Math.ceil(replyCount.value / pageSize.value);
});

const pageNumbers = computed(() => {
  const total = totalPages.value;
  const current = pageIndex.value;
  const pages = [];

  // 如果总页数<=5，显示全部
  if (total <= 5) {
    for (let i = 1; i <= total; i++) {
      pages.push(i);
    }
    return pages;
  }

  // 总页数>5时
  // 始终显示第1页
  pages.push(1);

  // 如果当前页离第1页很近（<=3），显示前4页
  if (current <= 3) {
    pages.push(2, 3, 4);
    pages.push("...");
    pages.push(total);
  }
  // 如果当前页离最后一页很近（>=total-2），显示后4页
  else if (current >= total - 2) {
    pages.push("...");
    pages.push(total - 3, total - 2, total - 1, total);
  }
  // 当前页在中间
  else {
    pages.push("...");
    pages.push(current - 1, current, current + 1);
    pages.push("...");
    pages.push(total);
  }

  return pages;
});

// 获取主题内容（sort=0的回复）
const mainContent = computed(() => {
  const mainReply = replies.value.find((r) => r.sort === 0);
  return mainReply?.content || topic.value?.content || "暂无内容";
});

// 过滤掉sort=0的回复，只显示真正的回复
const filteredReplies = computed(() => {
  return replies.value.filter((r) => r.sort !== 0);
});

// 直接显示过滤后的回复，筛选由后端处理
const displayedReplies = computed(() => {
  return filteredReplies.value;
});

// 生成返回列表页的URL，恢复之前的状态
const getListUrl = () => {
  const query = {};
  if (route.query.returnPage) {
    query.page = route.query.returnPage;
  }
  if (route.query.returnSearchKey) {
    query.searchKey = route.query.returnSearchKey;
  }
  if (route.query.returnCatalog) {
    query.catalog = route.query.returnCatalog;
  }
  return {
    path: "/",
    query,
  };
};

const toggleOnlyAuthor = () => {
  const mainContent = replies.value.find((r) => r.sort === 0);
  replies.value = mainContent ? [mainContent] : [];
  shouldAppend.value = false;
  router.push({
    path: `/topic/${tid.value}`,
    query: {
      page: 1,
      onlyAuthor: !onlyAuthor.value,
      onlyImage: false,
    },
  });
};

const toggleOnlyImage = () => {
  const mainContent = replies.value.find((r) => r.sort === 0);
  replies.value = mainContent ? [mainContent] : [];
  shouldAppend.value = false;
  router.push({
    path: `/topic/${tid.value}`,
    query: {
      page: 1,
      onlyAuthor: false,
      onlyImage: !onlyImage.value,
    },
  });
};

// 获取用户头像
const getUserAvatar = (uid) => {
  if (!uid || !users.value) return "";
  const user = users.value[uid];
  return user?.avatar || "";
};

// 获取引用回复信息
const getQuoteReply = (quotePid) => {
  if (!quotePid) return null;
  // 在 quoteReplies 中查找 pid 匹配的回复
  const replies = Object.values(quoteReplies.value);
  return replies.find((reply) => reply.pid === quotePid) || null;
};

// 获取引用用户信息
const getQuoteUser = (uid) => {
  if (!uid) return null;
  return quoteUsers.value[uid] || users.value[uid] || null;
};

// 截取引用内容（显示前100个字符）
const truncateContent = (content, maxLength = 100) => {
  if (!content) return "无内容";
  // 移除HTML标签
  const text = content
    .replace(/<[^>]*>/g, "")
    .replace(/&nbsp;/g, " ")
    .trim();
  if (text.length <= maxLength) return text;
  return text.substring(0, maxLength) + "...";
};

// 处理内容中的图片URL，解决跨域问题
const processContent = (content) => {
  if (!content) return "";

  // 处理图片链接，添加referrerPolicy
  let processed = content.replace(
    /<img([^>]*?)src=["']([^"']+)["']([^>]*?)>/gi,
    (match, before, src, after) => {
      // 如果是相对路径或已经是完整URL
      let imgSrc = src;

      // NGA图片通常需要referrer policy
      return `<img${before}src="${imgSrc}"${after} referrerpolicy="no-referrer" loading="lazy">`;
    },
  );

  return processed;
};

const fetchTopicDetail = async (append = false) => {
  try {
    const data = await getTopicDetail(tid.value, {
      PageIndex: pageIndex.value,
      PageSize: pageSize.value,
      OnlyAuthor: onlyAuthor.value,
      OnlyImage: onlyImage.value,
    });

    if (data) {
      topic.value = data.topic || {};

      if (append) {
        const newReplies = (data.replay?.data || []).filter(
          (r) => r.sort !== 0,
        );
        replies.value = [...replies.value, ...newReplies];
      } else {
        const mainContent = replies.value.find((r) => r.sort === 0);
        const newReplies = data.replay?.data || [];

        if (mainContent && !newReplies.some((r) => r.sort === 0)) {
          replies.value = [
            mainContent,
            ...newReplies.filter((r) => r.sort !== 0),
          ];
        } else {
          replies.value = newReplies;
        }
      }

      users.value = { ...users.value, ...(data.user || {}) };
      quoteReplies.value = {
        ...quoteReplies.value,
        ...(data.quoteReplay || {}),
      };
      quoteUsers.value = { ...quoteUsers.value, ...(data.quoteUser || {}) };
      replyCount.value = data.replay?.totalCount || 0;
    }
  } catch (error) {
    console.error("Failed to fetch topic detail:", error);
    errorMessage.value =
      error?.response?.data?.message ||
      error?.message ||
      "加载失败，请稍后重试";
    setTimeout(() => {
      errorMessage.value = "";
    }, 5000);
  }
};

// 监听路由变化，自动同步参数和刷新
watch(
  () => [route.query.page, route.query.onlyAuthor, route.query.onlyImage],
  (
    [newPage, newOnlyAuthor, newOnlyImage],
    [oldPage, oldOnlyAuthor, oldOnlyImage],
  ) => {
    const newPageNum = Number(newPage) || 1;

    // 检测筛选条件是否改变
    const filterChanged =
      newOnlyAuthor !== oldOnlyAuthor || newOnlyImage !== oldOnlyImage;

    if (filterChanged) {
      // 筛选条件改变时，清空列表并重置
      const mainContent = replies.value.find((r) => r.sort === 0);
      replies.value = mainContent ? [mainContent] : [];
      shouldAppend.value = false;
    }

    pageIndex.value = newPageNum;
    onlyAuthor.value = newOnlyAuthor === "true";
    onlyImage.value = newOnlyImage === "true";

    // 使用 shouldAppend 标志决定加载模式
    fetchTopicDetail(shouldAppend.value);

    // 重置标志
    if (shouldAppend.value) {
      shouldAppend.value = false;
    }
  },
);

const changePage = (page, isNextButton = false) => {
  // 判断是否是连续的下一页（无论是点击按钮还是点击页码）
  if (page === pageIndex.value + 1) {
    shouldAppend.value = true; // 连续下一页追加模式
  } else {
    shouldAppend.value = false; // 其他情况（上一页、跨页跳转）都刷新
  }

  router.push({
    path: `/topic/${tid.value}`,
    query: {
      page,
      onlyAuthor: onlyAuthor.value,
      onlyImage: onlyImage.value,
    },
  });

  if (!isNextButton) {
    window.scrollTo(0, 0);
  }
};

const formatTime = (dateStr) => {
  if (!dateStr) return "未知时间";
  try {
    let timestamp = parseInt(dateStr);
    // 如果是秒级时间戳（10位），转换为毫秒
    if (timestamp < 10000000000) {
      timestamp = timestamp * 1000;
    }
    const date = new Date(timestamp);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, "0");
    const day = String(date.getDate()).padStart(2, "0");
    const hours = String(date.getHours()).padStart(2, "0");
    const minutes = String(date.getMinutes()).padStart(2, "0");
    const seconds = String(date.getSeconds()).padStart(2, "0");
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
  } catch {
    return dateStr;
  }
};

const formatViews = (views) => {
  if (!views) return "0";
  const num = parseInt(views);
  if (num > 1000) {
    return (num / 1000).toFixed(1) + "K";
  }
  return num.toString();
};

onMounted(() => {
  fetchTopicDetail();
});
</script>

<style scoped>
* {
  box-sizing: border-box;
}

.topic-detail-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f0f2f5;
}

.main-content {
  max-width: 1000px;
  width: 100%;
  margin: 0 auto;
  padding: 20px 24px 40px;
  flex: 1;
  position: relative;
}

.error-toast {
  position: fixed;
  top: 80px;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 24px;
  background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%);
  color: #ffffff;
  border-radius: 12px;
  box-shadow: 0 8px 24px rgba(255, 107, 107, 0.4);
  z-index: 1000;
  animation: slideDown 0.3s ease-out;
  max-width: 90%;
  word-break: break-word;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateX(-50%) translateY(-20px);
  }
  to {
    opacity: 1;
    transform: translateX(-50%) translateY(0);
  }
}

.error-icon {
  font-size: 20px;
  flex-shrink: 0;
}

.error-text {
  flex: 1;
  font-size: 14px;
  font-weight: 500;
}

.error-close {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: #ffffff;
  font-size: 20px;
  width: 24px;
  height: 24px;
  border-radius: 50%;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  transition: background 0.3s;
  flex-shrink: 0;
}

.error-close:hover {
  background: rgba(255, 255, 255, 0.3);
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  margin-bottom: 16px;
  color: rgba(0, 0, 0, 0.45);
}

.breadcrumb a {
  color: rgba(0, 0, 0, 0.65);
  text-decoration: none;
  transition: color 0.3s;
}

.breadcrumb a:hover {
  color: #1890ff;
}

.breadcrumb .separator {
  color: rgba(0, 0, 0, 0.25);
  margin: 0 4px;
}

.breadcrumb .current {
  color: rgba(0, 0, 0, 0.85);
}

.detail-wrapper {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* 主题卡片 */
.topic-card {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  border: 1px solid rgba(0, 0, 0, 0.05);
}

.topic-header {
  padding: 24px 24px 16px;
  border-bottom: 2px solid #f0f0f0;
  background: linear-gradient(to bottom, #fafafa 0%, #ffffff 100%);
}

.title {
  font-size: 24px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.85);
  line-height: 1.4;
  margin: 0 0 16px 0;
}

.meta-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.author-section {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 14px;
}

.author-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.author {
  color: rgba(0, 0, 0, 0.85);
  font-weight: 500;
  font-size: 15px;
}

.divider {
  color: rgba(0, 0, 0, 0.25);
}

.time {
  color: rgba(0, 0, 0, 0.45);
  font-size: 13px;
}

.stats-section {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 14px;
  color: rgba(0, 0, 0, 0.45);
}

.stat-item .icon {
  color: rgba(0, 0, 0, 0.25);
}

.topic-body {
  padding: 24px;
}

.content {
  font-size: 15px;
  color: rgba(0, 0, 0, 0.85);
  line-height: 1.74;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

.content :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: 2px;
  margin: 16px 0;
  display: block;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.content :deep(p) {
  margin: 0 0 12px 0;
}

.content :deep(p:last-child) {
  margin-bottom: 0;
}

.content :deep(a) {
  color: #1890ff;
  text-decoration: none;
}

.content :deep(a:hover) {
  color: #40a9ff;
}

/* 回复区域 */
.replies-section {
  background: #fff;
  border-radius: 12px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
  overflow: hidden;
  border: 1px solid rgba(0, 0, 0, 0.05);
}

.section-header {
  padding: 16px 24px;
  border-bottom: 2px solid #f0f0f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: linear-gradient(to bottom, #fafafa 0%, #ffffff 100%);
}

.section-header h3 {
  margin: 0;
  font-size: 16px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.85);
}

.filter-buttons {
  display: flex;
  gap: 8px;
}

.filter-btn {
  padding: 6px 16px;
  font-size: 13px;
  color: rgba(0, 0, 0, 0.65);
  background: #fff;
  border: 1px solid #d9d9d9;
  border-radius: 16px;
  cursor: pointer;
  transition: all 0.3s;
}

.filter-btn:hover {
  color: #4a90e2;
  border-color: #4a90e2;
  background: rgba(93, 173, 226, 0.05);
}

.filter-btn.active {
  color: #fff;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  border-color: transparent;
  box-shadow: 0 2px 8px rgba(93, 173, 226, 0.3);
}

.reply-list {
  padding: 0;
}

.reply-item {
  display: flex;
  gap: 12px;
  padding: 16px 24px;
  border-bottom: 1px solid #f0f0f0;
  transition: all 0.3s;
  position: relative;
}

.reply-item::before {
  content: "";
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  opacity: 0;
  transition: opacity 0.3s;
}

.reply-item:hover {
  background-color: rgba(93, 173, 226, 0.03);
}

.reply-item:hover::before {
  opacity: 1;
}

.reply-item.reply-even {
  background-color: #ffffff;
}

.reply-item.reply-odd {
  background-color: #fafafa;
}

.reply-item:last-child {
  border-bottom: none;
}

.reply-item:hover {
  opacity: 0.9;
}

.reply-number {
  flex-shrink: 0;
  width: 40px;
  height: 24px;
  line-height: 24px;
  text-align: center;
  font-size: 12px;
  font-weight: 600;
  color: #667eea;
  background: linear-gradient(
    135deg,
    rgba(102, 126, 234, 0.1) 0%,
    rgba(118, 75, 162, 0.1) 100%
  );
  border-radius: 12px;
  border: 1px solid rgba(102, 126, 234, 0.2);
}

.reply-content-wrapper {
  flex: 1;
  min-width: 0;
}

.reply-header {
  display: flex;
  flex-direction: column;
  gap: 4px;
  margin-bottom: 8px;
}

.reply-header .author {
  font-size: 14px;
  font-weight: 600;
  color: rgba(0, 0, 0, 0.85);
}

.reply-header .time {
  font-size: 12px;
  color: rgba(0, 0, 0, 0.45);
}

.quote-box {
  margin-bottom: 12px;
  padding: 12px;
  background: linear-gradient(
    135deg,
    rgba(93, 173, 226, 0.05) 0%,
    rgba(52, 152, 219, 0.05) 100%
  );
  border-left: 3px solid #4a90e2;
  border-radius: 6px;
  transition: all 0.3s;
}

.quote-box:hover {
  background: linear-gradient(
    135deg,
    rgba(93, 173, 226, 0.08) 0%,
    rgba(52, 152, 219, 0.08) 100%
  );
  border-left-color: #5dade2;
}

.quote-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.quote-icon {
  font-size: 18px;
  color: #4a90e2;
  font-weight: bold;
}

.quote-author {
  font-size: 13px;
  font-weight: 600;
  color: #4a90e2;
}

.quote-floor {
  font-size: 12px;
  color: rgba(0, 0, 0, 0.45);
  padding: 2px 8px;
  background: rgba(74, 144, 226, 0.1);
  border-radius: 10px;
}

.quote-content {
  font-size: 13px;
  color: rgba(0, 0, 0, 0.65);
  line-height: 1.6;
  padding-left: 26px;
  font-style: italic;
  word-break: break-word;
}

.reply-body {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.85);
  line-height: 1.74;
  margin-bottom: 8px;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

.reply-body :deep(img) {
  max-width: 100%;
  height: auto;
  border-radius: 2px;
  margin: 12px 0;
  display: block;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.reply-body :deep(p) {
  margin: 0 0 8px 0;
}

.reply-body :deep(p:last-child) {
  margin-bottom: 0;
}

.reply-body :deep(a) {
  color: #1890ff;
  text-decoration: none;
}

.reply-body :deep(a:hover) {
  color: #40a9ff;
}

.reply-footer {
  display: flex;
  gap: 16px;
  margin-top: 8px;
}

.vote-item {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-size: 13px;
  color: rgba(0, 0, 0, 0.45);
  cursor: pointer;
  transition: all 0.3s;
  user-select: none;
}

.vote-item svg {
  flex-shrink: 0;
}

.vote-item.like:hover {
  color: #52c41a;
}

.vote-item.dislike:hover {
  color: #ff4d4f;
}

.action-btn .icon {
  width: 14px;
  height: 14px;
}

/* 空状态 */
.empty-state {
  padding: 60px 24px;
  text-align: center;
  color: rgba(0, 0, 0, 0.45);
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.empty-text {
  font-size: 16px;
  font-weight: 500;
  color: rgba(0, 0, 0, 0.65);
  margin-bottom: 8px;
}

.empty-hint {
  font-size: 14px;
  color: rgba(0, 0, 0, 0.45);
}

/* 分页 */
.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 8px;
  padding: 24px 0;
}

.page-btn,
.page-num {
  min-width: 32px;
  height: 36px;
  padding: 0 12px;
  font-size: 14px;
  color: rgba(0, 0, 0, 0.85);
  background: #fff;
  border: 1px solid #d9d9d9;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.3s;
  line-height: 34px;
}

.page-btn {
  padding: 0 16px;
}

.page-btn:hover:not(:disabled),
.page-num:hover {
  color: #4a90e2;
  border-color: #4a90e2;
  background: rgba(93, 173, 226, 0.05);
  transform: translateY(-2px);
}

.page-btn:disabled {
  color: rgba(0, 0, 0, 0.25);
  background: #f5f5f5;
  border-color: #d9d9d9;
  cursor: not-allowed;
  transform: none;
}

.page-num.active {
  color: #ffffff;
  background: linear-gradient(135deg, #5dade2 0%, #3498db 100%);
  border-color: transparent;
  font-weight: 600;
  box-shadow: 0 2px 8px rgba(93, 173, 226, 0.3);
}

.ellipsis {
  min-width: 32px;
  height: 32px;
  line-height: 32px;
  text-align: center;
  color: rgba(0, 0, 0, 0.25);
  user-select: none;
}

/* 响应式 */
@media (max-width: 768px) {
  .main-content {
    padding: 12px 16px 24px;
  }

  .topic-header {
    padding: 16px;
  }

  .title {
    font-size: 20px;
  }

  .topic-body {
    padding: 16px;
  }

  .reply-item {
    padding: 12px 16px;
  }

  .reply-number {
    display: none;
  }
}
</style>
