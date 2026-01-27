<template>
  <div class="topic-detail-page">
    <AppHeader />

    <main class="main-content">
      <div class="breadcrumb">
        <router-link to="/">首页</router-link>
        <span class="separator">></span>
        <router-link to="/">全部主题</router-link>
        <span class="separator">></span>
        <span class="current">主题详情</span>
      </div>

      <div v-if="topic" class="topic-card">
        <div class="topic-header">
          <div class="author-area">
            <div class="author-avatar"></div>
            <div class="author-info">
              <div class="author-name">{{ topic.uid || "用户" }}</div>
              <div class="post-time">
                发布于{{ formatTime(topic.postDate) }}
              </div>
            </div>
          </div>
          <div class="topic-metrics">
            <span>{{ replyCount }}条评论</span>
            <span>{{ formatViews(topic.thread) }}次阅读</span>
          </div>
        </div>

        <h1 class="topic-title">{{ topic.title }}</h1>

        <div class="topic-body" v-html="topic.content || '暂无内容'"></div>
      </div>

      <div v-if="replies.length > 0" class="replies-section">
        <div
          v-for="(reply, index) in replies"
          :key="reply.id"
          class="reply-card"
          :class="index % 2 === 0 ? 'reply-green' : 'reply-blue'"
        >
          <div class="reply-header">
            <div class="reply-left">
              <div
                class="reply-avatar"
                :class="index % 2 === 0 ? 'avatar-green' : 'avatar-blue'"
              ></div>
              <div class="reply-info">
                <div class="reply-author">{{ reply.uName || "用户" }}</div>
                <div class="reply-time">{{ formatTime(reply.postDate) }}</div>
              </div>
            </div>
            <div class="reply-actions">
              <span>👍 {{ reply.support || 0 }}</span>
              <span>👎 {{ reply.oppose || 0 }}</span>
            </div>
          </div>

          <div class="reply-content" v-html="reply.content"></div>
        </div>
      </div>

      <div class="pagination">
        <button
          class="pagination-btn"
          :disabled="pageIndex <= 1"
          @click="changePage(pageIndex - 1)"
        >
          上一页
        </button>
        <button class="pagination-num active">{{ pageIndex }}</button>
        <button
          class="pagination-btn"
          :disabled="pageIndex >= totalPages"
          @click="changePage(pageIndex + 1)"
        >
          下一页
        </button>
      </div>
    </main>

    <AppFooter />
  </div>
</template>

<script setup>
import { ref, onMounted, computed } from "vue";
import { useRoute } from "vue-router";
import { getTopicDetail } from "@/api/topic";
import AppHeader from "@/components/AppHeader.vue";
import AppFooter from "@/components/AppFooter.vue";

const route = useRoute();
const tid = ref(route.params.tid);

const topic = ref(null);
const replies = ref([]);
const users = ref({});
const pageIndex = ref(1);
const pageSize = ref(20);
const replyCount = ref(0);

const totalPages = computed(() => {
  return Math.ceil(replyCount.value / pageSize.value);
});

const fetchTopicDetail = async () => {
  try {
    const data = await getTopicDetail(tid.value, {
      PageIndex: pageIndex.value,
      PageSize: pageSize.value,
      OnlyAuthor: false,
      OnlyImage: false,
    });

    if (data) {
      topic.value = data.topic || {};
      replies.value = data.replay?.data || [];
      users.value = data.user || {};
      replyCount.value = data.replay?.totalCount || 0;
    }
  } catch (error) {
    console.error("Failed to fetch topic detail:", error);
  }
};

const changePage = (page) => {
  pageIndex.value = page;
  fetchTopicDetail();
  window.scrollTo(0, 0);
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
.topic-detail-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f8f9fa;
}

.main-content {
  max-width: 1440px;
  width: 100%;
  margin: 0 auto;
  padding: 32px 32px 0 32px;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0;
}

.breadcrumb {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
  margin-bottom: 24px;
}

.breadcrumb a {
  color: #2196f3;
}

.breadcrumb .separator {
  color: #999999;
}

.breadcrumb .current {
  color: #666666;
}

.topic-card {
  background: #ffffff;
  border-radius: 12px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
  margin-bottom: 0;
}

.topic-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.author-area {
  display: flex;
  gap: 12px;
  align-items: center;
}

.author-avatar {
  width: 48px;
  height: 48px;
  border-radius: 50%;
  background: #ffe0b2;
}

.author-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.author-name {
  font-size: 16px;
  font-weight: 600;
  color: #333333;
}

.post-time {
  font-size: 13px;
  color: #999999;
}

.topic-metrics {
  display: flex;
  gap: 20px;
  font-size: 14px;
  font-weight: 500;
  color: #666666;
}

.topic-title {
  font-size: 24px;
  font-weight: 700;
  color: #1a1a1a;
  margin-bottom: 16px;
  line-height: 1.4;
}

.topic-body {
  font-size: 15px;
  color: #444444;
  line-height: 1.6;
}

.replies-section {
  display: flex;
  flex-direction: column;
  gap: 0;
}

.reply-card {
  background: #ffffff;
  padding: 20px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.reply-card.reply-green {
  background: #f1f8e9;
}

.reply-card.reply-blue {
  background: #e3f2fd;
}

.reply-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.reply-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.reply-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
}

.reply-avatar.avatar-green {
  background: #c5e1a5;
}

.reply-avatar.avatar-blue {
  background: #b3e5fc;
}

.reply-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.reply-author {
  font-size: 14px;
  color: #333333;
}

.reply-time {
  font-size: 12px;
  color: #999999;
}

.reply-actions {
  display: flex;
  gap: 16px;
  font-size: 13px;
  color: #666666;
}

.reply-content {
  font-size: 14px;
  color: #444444;
  line-height: 1.6;
}

.pagination {
  display: flex;
  justify-content: center;
  gap: 8px;
  padding: 24px 0;
}

.pagination-btn,
.pagination-num {
  min-width: 80px;
  height: 36px;
  padding: 0 16px;
  background: #ffffff;
  border-radius: 6px;
  font-size: 14px;
  color: #666666;
  transition: all 0.2s;
}

.pagination-btn:hover:not(:disabled) {
  background: #f5f5f5;
}

.pagination-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.pagination-num {
  min-width: 36px;
  padding: 0;
}

.pagination-num.active {
  background: #ff9800;
  color: #ffffff;
  font-weight: 600;
}
</style>
