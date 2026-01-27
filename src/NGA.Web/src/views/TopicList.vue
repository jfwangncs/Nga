<template>
  <div class="topic-list-page">
    <AppHeader />

    <main class="main-content">
      <div class="search-bar">
        <span class="search-icon">🔍</span>
        <input
          v-model="searchKey"
          type="text"
          placeholder="通过关键词或标题搜索..."
          @keyup.enter="handleSearch"
        />
      </div>

      <div class="pagination-info">
        <span>共显示 {{ totalCount }} 个主题</span>
        <span>第 {{ pageIndex }} 页，共 {{ totalPages }} 页</span>
      </div>

      <div class="topic-list">
        <div
          v-for="topic in topics"
          :key="topic.id"
          class="topic-card"
          @click="goToDetail(topic.tid)"
        >
          <div class="topic-header">
            <div class="topic-left">
              <div class="topic-info">
                <div class="topic-author">
                  {{ topic.userName || topic.uid || "用户" }}
                </div>
                <div class="topic-time">{{ formatTime(topic.postDate) }}</div>
              </div>
            </div>
            <div class="topic-stats">
              <span>{{ topic.replies || 0 }}条回复</span>
            </div>
          </div>

          <h3 class="topic-title">{{ topic.title }}</h3>

          <div class="topic-tags" v-if="topic.fid">
            <span class="tag">{{ topic.fid }}</span>
          </div>
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

        <template v-for="page in pageNumbers" :key="page">
          <button
            v-if="page !== '...'"
            class="pagination-num"
            :class="{ active: pageIndex === page }"
            @click="changePage(page)"
          >
            {{ page }}
          </button>
          <span v-else class="pagination-ellipsis">...</span>
        </template>

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
import { useRouter } from "vue-router";
import { getTopicList } from "@/api/topic";
import AppHeader from "@/components/AppHeader.vue";
import AppFooter from "@/components/AppFooter.vue";

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

const router = useRouter();

const topics = ref([]);
const pageIndex = ref(1);
const pageSize = ref(10);
const totalCount = ref(0);
const searchKey = ref("");

const totalPages = computed(() => {
  return Math.ceil(totalCount.value / pageSize.value);
});

const fetchTopics = async () => {
  try {
    const data = await getTopicList({
      PageIndex: pageIndex.value,
      PageSize: pageSize.value,
      SearchKey: searchKey.value,
    });

    if (data) {
      topics.value = data.data || [];
      totalCount.value = data.totalCount || 0;
    }
  } catch (error) {
    console.error("Failed to fetch topics:", error);
  }
};

const handleSearch = () => {
  pageIndex.value = 1;
  fetchTopics();
};

const changePage = (page) => {
  pageIndex.value = page;
  fetchTopics();
};

const goToDetail = (tid) => {
  router.push(`/topic/${tid}`);
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
  fetchTopics();
});
</script>

<style scoped>
.topic-list-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #f8f9fa;
}

.main-content {
  max-width: 1440px;
  width: 100%;
  margin: 0 auto;
  padding: 32px;
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.search-bar {
  width: 100%;
  height: 48px;
  background: #ffffff;
  border-radius: 8px;
  padding: 12px 16px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.search-icon {
  font-size: 16px;
  color: #666666;
}

.search-bar input {
  flex: 1;
  border: none;
  outline: none;
  font-size: 14px;
  color: #333333;
}

.search-bar input::placeholder {
  color: #999999;
}

.pagination-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  color: #666666;
}

.topic-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.topic-card {
  background: #ffffff;
  border-radius: 8px;
  padding: 20px;
  cursor: pointer;
  transition:
    transform 0.2s,
    box-shadow 0.2s;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
}

.topic-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12);
}

.topic-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.topic-left {
  display: flex;
  gap: 12px;
  align-items: center;
}

.topic-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: #ffe0b2;
}

.topic-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.topic-author {
  font-size: 14px;
  font-weight: 600;
  color: #333333;
}

.topic-time {
  font-size: 12px;
  color: #999999;
}

.topic-stats {
  display: flex;
  gap: 16px;
  font-size: 12px;
  color: #666666;
}

.topic-title {
  font-size: 16px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 12px;
}

.topic-tags {
  display: flex;
  gap: 8px;
}

.tag {
  padding: 4px 8px;
  background: #e3f2fd;
  color: #2196f3;
  font-size: 12px;
  border-radius: 4px;
}

.pagination-ellipsis {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 36px;
  height: 36px;
  font-size: 14px;
  color: #999999;
  user-select: none;
}
.pagination {
  display: flex;
  justify-content: center;
  gap: 8px;
  margin-top: auto;
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
  background: #2196f3;
  color: #ffffff;
  font-weight: 600;
}
</style>
